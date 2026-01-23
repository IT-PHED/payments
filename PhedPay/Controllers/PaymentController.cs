using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PhedPay.Data;
using PhedPay.Models;
using PhedPay.Services;
using System.Diagnostics.Eventing.Reader;
using System.Text;


namespace PhedPay.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppDbContext _context;
        private readonly AppDbOracleContext _oracleContext;
        private readonly PdfService _pdfService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IHttpClientFactory httpClientFactory,
            AppDbContext context, PdfService pdfService,
            IConfiguration configuration, ILogger<PaymentController> logger,
            AppDbOracleContext oracleContext)
        {
            _httpClientFactory = httpClientFactory;
            _context = context;
            _pdfService = pdfService;
            _configuration = configuration;
            _logger = logger;
            _oracleContext = oracleContext;
        }

        // 1. Initial Page
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // 2. Validate Customer
        [HttpPost]
        public async Task<IActionResult> ValidateCustomer(PaymentInitiationViewModel model)
        {
            if (!ModelState.IsValid) return View("Index", model);

            var client = _httpClientFactory.CreateClient();

            var requestPayload = new CustomerLookupRequest
            {
                Username = _configuration["api_username"],
                apikey = _configuration["apikey"],
                CustomerNumber = model.MeterNo,
                Mobile_Number = model.PhoneNumber,
                Mailid = model.Email
            };

            var content = new StringContent(JsonConvert.SerializeObject(requestPayload), Encoding.UTF8, "application/json");

            // Call PHED Customer Info API
            string baseUrl = _configuration["baseAPI"];
            string endpoint = $"{baseUrl}Collection/GetcustomerInfo";

            var response = await client.PostAsync(endpoint, content);


            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();

                // 1. Deserialize as a List because the JSON starts with '['
                var customerList = JsonConvert.DeserializeObject<List<dynamic>>(responseString);

                if (customerList != null && customerList.Count > 0)
                {
                    var data = customerList[0]; // Access the first object in the array

                    // 2. Map fields based on the ALL CAPS keys in your JSON
                    ViewBag.CustomerName = (string)data.CONS_NAME;
                    ViewBag.AccountNo = (string)data.CUSTOMER_NO;
                    ViewBag.MeterNo = (string)data.METER_NO;
                    ViewBag.Address = (string)data.ADDRESS;
                    ViewBag.FactorAmount = (string)data.FACTOR_AMOUNT;

                    // 3. Logic to deduce PREPAID vs POSTPAID
                    string rawType = (string)data.CONS_TYPE;
                    if (!string.IsNullOrEmpty(rawType))
                    {
                        if (rawType.ToUpper().Contains("PREPAID"))
                        {
                            ViewBag.AccountType = "PREPAID";
                        }
                        else if (rawType.ToUpper().Contains("POSTPAID"))
                        {
                            ViewBag.AccountType = "POSTPAID";
                        }
                        else
                        {
                            ViewBag.AccountType = "UNKNOWN";
                        }
                    }

                    // 4. Pass through the user's original inputs
                    ViewBag.Phone = model.PhoneNumber;
                    ViewBag.Email = model.Email;
                    ViewBag.Amount = model.Amount;

                    return View("ConfirmPayment");
                }
            }
            ModelState.AddModelError("", "Could not validate customer details.");
            return View("Index", model);
        }
        // 3. Initialize Payment (Proceed clicked)
        [HttpPost]
        public async Task<IActionResult> InitializePayment(string AccountNo, string meterNo, string email, string phone, decimal amount, string customerName, string address)
        {
            //var txId = Guid.NewGuid().ToString();
            var txId = $"{AccountNo}_{DateTime.Now.ToString("HHmmssffffff")}";

            // A. Save to Database
            var transaction = new TransactionEntity
            {
                // Do NOT set Id (It is auto-increment)
                // Do NOT set GlobalId (SQL handles it, or set Guid.NewGuid() if you prefer)
                TransactionReference = txId, // Store the "Account_Time" here
                AccountNo = AccountNo,
                MeterNo = meterNo,
                RefId = Guid.NewGuid(),
                Email = email,
                Phone = phone,
                Amount = amount,
                CustomerName = customerName,
                Address = address,
                CreatedDate = DateTime.Now,
                Status = "Pending"
            };
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            var client = _httpClientFactory.CreateClient();
            var callBackUrl = _configuration["CALLBACK_URL"] + AccountNo;

            var payload = new
            {
                amount = amount.ToString(),  // Send as number (e.g., 500.00). If API fails, change to: amount.ToString()
                email = email,
                transactionId = txId,
                currency = "NGN",
                productId = "1001",
                productDescription = "Payment for PHED Energy",
                callBackUrl = callBackUrl,
            };

            // 3. Serialize
            var jsonContent = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // 4. Build Request
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://myxpresspay.com:6004/api/Payments/Initialize");
            requestMessage.Headers.Add("Authorization", "Bearer XPPUBK-44502a361d384988b6fb0be47eefc789-X");
            requestMessage.Content = content;

            // 5. Send
            var response = await client.SendAsync(requestMessage);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();

                // Deserialize the JSON response
                var xpressResponse = JsonConvert.DeserializeObject<XpressPayResponse>(result);

                // Check if the gateway returned "00" (Success) and has a URL
                if (xpressResponse != null && xpressResponse.responseCode == "00" && !string.IsNullOrEmpty(xpressResponse.data?.paymentUrl))
                {
                    // THIS IS THE KEY CHANGE:
                    // Redirect the user to the external payment page
                    return Redirect(xpressResponse.data.paymentUrl);
                }
                else
                {
                    // The API responded (200 OK), but the content indicated a logic failure
                    ModelState.AddModelError("", $"Gateway Error: {xpressResponse?.responseMessage}");
                    return View("Error");
                }
            }
            else
            {
                // 6. IMPORTANT: Read the error message to see WHY it failed
                var errorBody = await response.Content.ReadAsStringAsync();

                // This will print the actual error from the API to your Visual Studio Output window
                _logger.LogInformation($"API Error: {0}", errorBody);

                // Return the error to the view so you can see it in the browser
                ModelState.AddModelError("", $"Payment Failed: {errorBody}");
                return View("Error");
            }


        }

        // 4. Success / Callback Handler

        [HttpGet]
        public async Task<IActionResult> VerifyAndProcess(string transactionId)
        {
            _logger.LogInformation("it hits here from {0}", nameof(PaymentController));

            if (string.IsNullOrEmpty(transactionId))
            {
                return BadRequest("No transaction ID provided.");
            }

            // 1. Retrieve Transaction from Local DB
            var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.TransactionReference == transactionId);

            if (transaction == null) return NotFound($"Transaction {transactionId} not found in database.");

            
            var client = _httpClientFactory.CreateClient();

            var verifyPayload = new { transactionId = transactionId }; // Anonymous object for JSON
            var verifyContent = new StringContent(JsonConvert.SerializeObject(verifyPayload), Encoding.UTF8, "application/json");

            var verifyRequest = new HttpRequestMessage(HttpMethod.Post, "https://myxpresspay.com:6004/api/Payments/VerifyPayment");
            verifyRequest.Headers.Add("Authorization", "Bearer XPPUBK-44502a361d384988b6fb0be47eefc789-X"); // REPLACE WITH REAL KEY
            verifyRequest.Content = verifyContent;

            var verifyResponse = await client.SendAsync(verifyRequest);
            var verifyResultJson = await verifyResponse.Content.ReadAsStringAsync();

            // Deserialize to check status (Assuming same structure as Init response)
            var verifyResult = JsonConvert.DeserializeObject<XpressPayResponse>(verifyResultJson);

            _logger.LogInformation("payload response -- {0}", JsonConvert.SerializeObject(verifyResult));

            // 3. CHECK IF SUCCESSFUL
            if (verifyResponse.IsSuccessStatusCode && verifyResult?.responseCode == "00")
            {
                
                transaction.Status = "Success";
                await _context.SaveChangesAsync();

              
                return RedirectToAction("Receipt", new { id = transaction.RefId });
            }
            else
            {
                // Payment failed or verification failed
                transaction.Status = "Failed";
                await _context.SaveChangesAsync();

                ModelState.AddModelError("", $"Payment verification failed: {verifyResult?.responseMessage ?? "Unknown Error"}");
                return View("Error");
            }
        }

        private async Task<string> NotifyPhedBackend(TransactionEntity tx)
        {
            var client = _httpClientFactory.CreateClient();
            var now = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");

            var notifyPayload = new PhedNotificationRequest
            {
                Username = _configuration["api_username"],
                apikey = _configuration["apikey"],



                // Safety: If TransactionReference is missing, don't crash, send empty (though this shouldn't happen)
                PaymentLogId = tx.TransactionReference ?? "",
                CustReference = tx.AccountNo ?? tx.MeterNo ?? "",
                AlternateCustReference = tx.MeterNo ?? "",

                Amount = tx.Amount.ToString("0.00"),
                PaymentMethod = "WEB",
                PaymentReference = tx.TransactionReference ?? "",
                TerminalID = "WebTerminal",
                ChannelName = "WEB",

                // CRITICAL FIX: Send "" instead of null
                Location = "",

                PaymentDate = now, // Ensure 'now' is "dd-MM-yyyy HH:mm:ss"

                // CRITICAL FIX: These caused the crash likely
                InstitutionId = "",
                InstitutionName = "",

                BankName = "PHED WEB",
                BranchName = "PHED WEB",
                CustomerName = tx.CustomerName ?? "Unknown",

                // CRITICAL FIX: Send "" instead of null
                OtherCustomerInfo = "",

                ReceiptNo = tx.TransactionReference ?? "",
                CollectionsAccount = "", // "" instead of null
                BankCode = "023",
                CustomerAddress = tx.Address ?? "",
                CustomerPhoneNumber = tx.Phone ?? "",
                DepositorName = tx.CustomerName ?? "Unknown",
                DepositSlipNumber = "0",
                PaymentCurrency = "NGN",
                ItemName = "PHED Bill Payment",
                ItemCode = "01",
                ItemAmount = tx.Amount.ToString("0.00"),
                PaymentStatus = "Success",

                // CRITICAL FIX: Send string "False", not null
                IsReversal = "False",

                SettlementDate = now,
                Teller = "PHED WebTeller"
            };


            var content = new StringContent(JsonConvert.SerializeObject(notifyPayload), Encoding.UTF8, "application/json");
            string token = "";

            try
            {
                string baseUrl = _configuration["baseAPI"];
                string endpoint = $"{baseUrl}Collection/NotifyPayment";
                var resp = await client.PostAsync(endpoint, content);

                var output = resp;
                var errorBody = await resp.Content.ReadAsStringAsync();

                _logger.LogInformation("API Error: {0}", errorBody);

            }
            catch (Exception ex)
            {
                _logger.LogInformation("PHED Notification Failed: {0}", ex.Message);
              
            }
            return token;
        }
        public async Task<String> VerifyAndProcessPending(string transactionId)
        {
           
            var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.TransactionReference == transactionId);

            if (transaction == null) return ($"Transaction {transactionId} not found in database.");

            // 2. VERIFY PAYMENT (Call XpressPay)

            //if it contains GP, skip this and call GlobalPay requery instead
            if (transactionId.StartsWith("GP", StringComparison.OrdinalIgnoreCase))
            {
                await VerifyGlobalPay(transactionId);
                return "";
            }

            var client = _httpClientFactory.CreateClient();

            var verifyPayload = new { transactionId = transactionId }; // Anonymous object for JSON
            var verifyContent = new StringContent(JsonConvert.SerializeObject(verifyPayload), Encoding.UTF8, "application/json");

            var verifyRequest = new HttpRequestMessage(HttpMethod.Post, "https://myxpresspay.com:6004/api/Payments/VerifyPayment");
            verifyRequest.Headers.Add("Authorization", "Bearer XPPUBK-44502a361d384988b6fb0be47eefc789-X"); // REPLACE WITH REAL KEY
            verifyRequest.Content = verifyContent;

            var verifyResponse = await client.SendAsync(verifyRequest);
            var verifyResultJson = await verifyResponse.Content.ReadAsStringAsync();

            // Deserialize to check status (Assuming same structure as Init response)
            var verifyResult = JsonConvert.DeserializeObject<XpressPayResponse>(verifyResultJson);

            // 3. CHECK IF SUCCESSFUL
            if (verifyResponse.IsSuccessStatusCode && verifyResult?.responseCode == "00")
            {
                // Update Local DB
                transaction.Status = "Success";
                await _context.SaveChangesAsync();

                // 4. NOTIFY PHED BACKEND
               var token = await NotifyPhedBackend(transaction);

                // 5. SHOW RECEIPT
               // return View("Receipt", transaction);
            }
            else
            {
                // Payment failed or verification failed
                transaction.Status = "Failed";
                await _context.SaveChangesAsync();
                ModelState.AddModelError("", $"Payment verification failed: {verifyResult?.responseMessage ?? "Unknown Error"}");
               
            }

            return "OK";
        }

        [HttpGet]
        public async Task<IActionResult> VerifyGlobalPay(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId)) return BadRequest("No Transaction ID provided.");

          
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.TransactionReference == transactionId);

            if (transaction == null) return NotFound($"Transaction {transactionId} not found in database.");

            // 2. IDEMPOTENCY CHECK (Prevent duplicate processing)
            // If we already marked it as success, skip the APIs and show the receipt immediately.
            if (transaction.Status == "Success")
            {
                return View("Receipt", transaction);
            }

            // 3. REQUERY GLOBALPAY (Convert Node.js logic to C#)
            var client = _httpClientFactory.CreateClient();

            // Construct the URL: BaseURL + Path + TransactionRef
            // BaseURL usually: https://paygw.globalpay.com.ng/globalpay-paymentgateway/api
            
            var baseUrl = _configuration["GlobalPaySettings:baseUrl"] ?? "your-api-key"; 
            var requestUrl = $"{baseUrl}/paymentgateway/query-single-transaction-by-merchant-reference/{transactionId}";

            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

            // Add Headers from Config
            var apiKey = _configuration["GlobalPaySettings:ApiKey"] ?? "your-api-key";
            request.Headers.Add("apiKey", apiKey);

            try
            {
                // ... inside the try block ...
                var response = await client.SendAsync(request);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var gpResult = JsonConvert.DeserializeObject<GlobalPayQueryResponse>(responseString);

                    // FIX 3: Check if List is not null and has items
                    var paymentData = gpResult?.data?.FirstOrDefault();

                    // FIX 4: Validate based on the item found
                    if (gpResult != null && gpResult.isSuccessful &&
                        paymentData != null &&
                        paymentData.transactionStatus?.Equals("Successful", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        // A. Update Local DB
                        transaction.Status = "Success";
                        await _context.SaveChangesAsync();

                        // B. Notify PHED
                        await NotifyPhedBackend(transaction);

                        // C. Show Receipt
                        return View("Receipt", transaction);
                    }
                    else
                    {
                        // Payment Failed
                        transaction.Status = "Failed";
                        await _context.SaveChangesAsync();

                        var errorMsg = paymentData?.transactionStatus ?? gpResult?.successMessage ?? "Payment Failed";
                        ModelState.AddModelError("", $"Payment Unsuccessful: {errorMsg}");
                        return View("Error");
                    }
                }
                return View("Error");
            }
            catch (Exception ex)
            {
                // Network or Code Error
                System.Diagnostics.Debug.WriteLine($"VerifyGlobalPay Exception: {ex.Message}");
                ModelState.AddModelError("", "An error occurred during verification.");
                return View("Error");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ProcessPending()
        {
            var transactions = await _context.Transactions
                .Where(t => t.Status == "Pending")
                .ToListAsync();

            if (!transactions.Any())
                return NotFound("No pending transactions found.");

            foreach (var tra in transactions)
            {
                await VerifyAndProcessPending(tra.TransactionReference);
            }

            return Ok(new
            {
                Message = "Pending transactions processed successfully.",
                Count = transactions.Count
            });
        }

        [HttpPost]
        public async Task<IActionResult> RetryProcessPending(string transactionReference)
        {
            if (string.IsNullOrWhiteSpace(transactionReference))
                return BadRequest("TransactionReference is required.");

            await VerifyAndProcessPending(transactionReference);

            return Ok(new
            {
                Message = "Pending transactions processed successfully."
            });
        }


        [HttpGet]
        public async Task<IActionResult> Requery(string AccountNo)
        {
            if (string.IsNullOrEmpty(AccountNo)) return View(new RequeryViewModel());

            // 1. Get Local Transactions
            var transactions = await _context.Transactions
                .Where(t => t.AccountNo == AccountNo || t.MeterNo == AccountNo)
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();

            // 2. Process Pending Transactions (Your existing logic)
            // We do this BEFORE fetching Oracle data to ensure the Oracle list includes any just-processed payments
           foreach (var tra in transactions.Where(t => t.Status  == "Pending" || t.Status == "Failed" && t.CreatedDate > DateTime.Now.AddDays(-30)))
            {
                await VerifyAndProcessPending(tra.TransactionReference);
            }

            // 3. Reload Local Transactions (to reflect Status changes after processing)
            transactions = await _context.Transactions
                .Where(t => t.AccountNo == AccountNo || t.MeterNo == AccountNo)
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();

            // 4. Get Oracle Payments safely
            // Note: I simplified the logic to rely on the input AccountNo to avoid crashing if transactions is empty
            var payments = new List<Payment>();
            try
            {
                payments = await _oracleContext.Payments
                    .Where(p => (p.ConsumerNo == AccountNo || p.MeterNo == AccountNo) && p.CreatedBy == "phed")
                    .OrderByDescending(p => p.PaymentDateTime) // Assuming you have a date column
                    .Take(50) // Limit to last 50 to prevent huge loads
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                // Log Oracle error but don't crash the page, just show empty Oracle list
                Console.WriteLine($"Oracle Error: {ex.Message}");
            }

            // 5. Build ViewModel
            var model = new RequeryViewModel
            {
                SearchKey = AccountNo,
                LocalTransactions = transactions,
                OraclePayments = payments
            };

            return View(model);
        }

        // 5. Download PDF

        [HttpGet]
        public async Task<IActionResult> DownloadReceipt(string transactionId)
        {
            // 1. Get Local Transaction to verify it exists
            // Note: Use TransactionReference (e.g. 8241_123...) not the numeric Id
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.TransactionReference == transactionId);

            if (transaction == null) return NotFound("Transaction not found.");

            // 2. Call PHED API to get the Official Receipt Data
            var client = _httpClientFactory.CreateClient();

            var requestPayload = new
            {

                Username = _configuration["api_username"],
                apikey = _configuration["apikey"],
                TransactionNo = transactionId // Use the ID stored in DB: "824111122001_124024985083"

            };

            var content = new StringContent(JsonConvert.SerializeObject(requestPayload), Encoding.UTF8, "application/json");

            try
            {
                string baseUrl = _configuration["baseAPI"];
                string endpoint = $"{baseUrl}Collection/GetTransactionInfo";
                var response = await client.PostAsync(endpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var receiptList = JsonConvert.DeserializeObject<List<PhedReceiptItem>>(jsonString);

                    if (receiptList != null && receiptList.Count > 0)
                    {
                        // 3. Generate PDF with API Data
                        var receiptData = receiptList[0];

                        // Fallback: Fill in any nulls from our local DB if the API returns nulls
                        if (string.IsNullOrEmpty(receiptData.CONS_NAME)) receiptData.CONS_NAME = transaction.CustomerName;
                        if (string.IsNullOrEmpty(receiptData.ADDRESS)) receiptData.ADDRESS = transaction.Address;

                        var pdfFile = _pdfService.GenerateOfficialReceipt(receiptData);
                        return File(pdfFile, "application/pdf", $"PHED_Receipt_{transactionId}.pdf");
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error
            }

            return BadRequest("Could not retrieve receipt from PHED. Please contact support.");
        }

        [HttpGet]
        public async Task<IActionResult> Receipt(Guid id)
        {
            try
            {
                var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.RefId == id);

                if (transaction == null)
                    return NotFound();

                if (!transaction.IsSynced && transaction.Status == "Success")
                {
                    // Notify PHED backend
                    await NotifyPhedBackend(transaction);

                    // Update sync status
                    transaction.IsSynced = true;
                    transaction.SyncedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return View(transaction);
            }
            catch (Exception ex)
            {
                // Log error but still show receipt
                _logger.LogError(ex, "Error notifying PHED backend for transaction {TransactionId}", id);

                // Get transaction without sync attempt
                var transaction = await _context.Transactions.FindAsync(id);
                return View(transaction);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetReceiptDetails(string transactionId)
        {
            // 1. Get transaction to find the correct TransactionReference
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.TransactionReference == transactionId);

            if (transaction == null) return NotFound("Transaction not found.");

            // 2. Call PHED API
            var client = _httpClientFactory.CreateClient();
            var requestPayload = new
            {
                Username = _configuration["api_username"],
                apikey = _configuration["apikey"],
                TransactionNo = transactionId
            };

            var content = new StringContent(JsonConvert.SerializeObject(requestPayload), Encoding.UTF8, "application/json");

           // var response = await client.PostAsync("https://collections.phed.ng/dlenhanceapi/Collection/GetTransactionInfo", content);
            string baseUrl = _configuration["baseAPI"];
            string endpoint = $"{baseUrl}Collection/GetTransactionInfo";
            var response = await client.PostAsync(endpoint, content);

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                // Return the raw JSON from PHED directly to the frontend
                return Content(jsonString, "application/json");
            }

            return BadRequest();
        }
        [HttpPost]
        public async Task<IActionResult> InitializeGlobalPay(string AccountNo, string meterNo, string email, string phone, decimal amount, string customerName, string address)
        {
            // 1. Generate Transaction Reference
            var txRef = $"GP_{DateTime.Now:yyyyMMddHHmmss}_{AccountNo}";
            // 1. Clean the raw input first
            string cleanName = (customerName ?? "").Trim();

            // 2. Default values (Fallback)
            string firstName = "Customer";
            string lastName = ".";

            if (!string.IsNullOrEmpty(cleanName))
            {
                // Split by space, and REMOVE EMPTY ENTRIES (handles double spaces)
                var parts = cleanName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length > 0)
                {
                    firstName = parts[0].Trim();

                    if (parts.Length > 1)
                    {
                        // Join the rest and TRIM the result
                        // This fixes the " DUMUOYI" issue
                        lastName = string.Join(" ", parts.Skip(1)).Trim();
                    }
                }
            }

           

              // 3. Prepare Payload
            var payload = new GlobalPayRequest
            {
                amount = amount,
                merchantTransactionReference = txRef,
                redirectUrl = Url.Action("VerifyGlobalPay", "Payment", new { transactionId = txRef }, Request.Scheme),
                customer = new GlobalPayCustomer
                {
                    firstName = firstName ?? "First_name",
                    lastName = lastName ?? "last_name",
                    currency = "NGN",
                    phoneNumber = phone,
                    address = address ?? "Port Harcourt", // Default if empty
                    emailAddress = email,
                    paymentFormCustomFields = new List<GlobalPayCustomField>
            {
                new GlobalPayCustomField { name = "AccountNo", value = AccountNo },
                new GlobalPayCustomField { name = "MeterNo", value = meterNo }
            }
                }
            };

            // 4. Send Request
            var client = _httpClientFactory.CreateClient();

            var apiKey = _configuration["GlobalPaySettings:ApiKey"] ?? "";
            var url = _configuration["GlobalPaySettings:Url"] ?? "";

            var requestMsg = new HttpRequestMessage(HttpMethod.Post, url);
            requestMsg.Headers.Add("apikey", apiKey);
            requestMsg.Headers.Add("language", "en"); 

            var jsonContent = JsonConvert.SerializeObject(payload);
            requestMsg.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            string whatImSending = await requestMsg.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine("FINAL REQUEST BODY: " + whatImSending);

            try
            {
                var response = await client.SendAsync(requestMsg);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    
                    var result = JsonConvert.DeserializeObject<GlobalPayInitResponse>(responseString);

                    
                    if (result != null && result.isSuccessful && result.data != null)
                    {
                      var transaction = new TransactionEntity
                        {
                            TransactionReference = txRef, // The ID we generated earlier
                            AccountNo = AccountNo,
                            MeterNo = meterNo,
                            Email = email,
                            Phone = phone,
                            Amount = amount,
                            CustomerName = customerName,
                            Address = address,
                            Status = "Pending",
                            CreatedDate = DateTime.Now
                        };

                        _context.Transactions.Add(transaction);
                        await _context.SaveChangesAsync();

                        // 4. Redirect User to GlobalPay Checkout
                        return Redirect(result.data.checkoutUrl);
                    }
                }

                // Log failure details for debugging
                System.Diagnostics.Debug.WriteLine("GlobalPay Failed: " + responseString);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GlobalPay Exception: " + ex.Message);
            }

            // Fallback if something went wrong
            return View("Error");

            
        }

    }
}



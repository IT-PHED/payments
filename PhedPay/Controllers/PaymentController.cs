using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PhedPay.Data;
using PhedPay.Models;
using PhedPay.Services;
using System.Text;


namespace PhedPay.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppDbContext _context;
        private readonly PdfService _pdfService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IHttpClientFactory httpClientFactory, AppDbContext context, PdfService pdfService, IConfiguration configuration, ILogger<PaymentController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _context = context;
            _pdfService = pdfService;
            _configuration = configuration;
            _logger = logger;
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
            var callBackUrl = _configuration["CALLBACK_URL"];

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

            //if (transaction.Status == "Success")
            //{
            //    return View("Receipt", transaction);
            //}

            // 2. VERIFY PAYMENT (Call XpressPay)
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
                // 4. NOTIFY PHED BACKEND
                //await NotifyPhedBackend(transaction);

                // Update Local DB
                transaction.Status = "Success";
                await _context.SaveChangesAsync();

                // 5. SHOW RECEIPT
                //return View("Receipt", transaction);
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

        private async Task NotifyPhedBackend(TransactionEntity tx)
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


            try
            {
                string baseUrl = _configuration["baseAPI"];
                string endpoint = $"{baseUrl}Collection/NotifyPayment";
                var resp = await client.PostAsync(endpoint, content);

                var output = resp;
                var errorBody = await resp.Content.ReadAsStringAsync();

                // This will print the actual error from the API to your Visual Studio Output window
                _logger.LogInformation("API Error: {0}", errorBody);

            }
            catch (Exception ex)
            {
                _logger.LogInformation("PHED Notification Failed: {0}", ex.Message);
                // Log failure to notify PHED, but payment is already successful
            }
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
    }
}



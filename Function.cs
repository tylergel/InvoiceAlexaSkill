using System;
using System.Collections.Generic;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using AlexaAPI;
using AlexaAPI.Request;
using AlexaAPI.Response;
using System.IO;
using System.Text.RegularExpressions;
using System.Net;
using System.Text;
using System.Collections.Specialized;

[assembly: LambdaSerializerAttribute(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace sampleFactCsharp
{
    public class Function
    {


        public class InvoiceObject
        {
            public string customer_email { get; set; }
            public string customer_name { get; set; }
            public string quote_id { get; set; }
            public string company_name { get; set; }
            public string id { get; set; }

            public string customer_street_address { get; set; }

            public string customer_phone_number { get; set; }
            public string customer_company_name { get; set; }
        }


        public class QuoteObject
        {
            public string id { get; set; }
        }
        public class CompanyObject
        {
            public string id { get; set; } 
            public string quote_id { get; set; }
            public string company_name { get; set; } 

            public string street_address { get; set; }

            public string phone_number { get; set; }
            public string email { get; set; }
        }
        private SkillResponse response = null;
        private ILambdaContext context = null;
        const string LOCALENAME = "locale";
        const string USA_Locale = "en-US";

        static Random rand = new Random();

        /// <summary>
        /// Application entry point
        /// </summary>
        /// <param name="input"></param>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public SkillResponse FunctionHandler(SkillRequest input, ILambdaContext ctx)
        {
            context = ctx;
            try
            {
                response = new SkillResponse();
                response.Response = new ResponseBody();
                response.Response.ShouldEndSession = false;
                response.Version = AlexaConstants.AlexaVersion;

                if (input.Request.Type.Equals(AlexaConstants.LaunchRequest))
                {
                    string locale = input.Request.Locale;
                    if (string.IsNullOrEmpty(locale))
                    {
                        locale = USA_Locale;
                    }

                    ProcessLaunchRequest(response.Response);
                    response.SessionAttributes = new Dictionary<string, object>() {{LOCALENAME, locale}};
                }
                else
                {
                    if (input.Request.Type.Equals(AlexaConstants.IntentRequest))
                    {
                       string locale = string.Empty;
                       Dictionary <string, object> dictionary = input.Session.Attributes;
                       if (dictionary != null)
                       {
                           if (dictionary.ContainsKey(LOCALENAME))
                           {
                               locale = (string) dictionary[LOCALENAME];
                           }
                       }
               
                       if (string.IsNullOrEmpty(locale))
                       {
                            locale = input.Request.Locale;
                       }

                       if (string.IsNullOrEmpty(locale))
                       {
                            locale = USA_Locale; 
                       }

                       response.SessionAttributes = new Dictionary<string, object>() {{LOCALENAME, locale}};

                       if (IsDialogIntentRequest(input))
                       {
                            if (!IsDialogSequenceComplete(input))
                            { // delegate to Alexa until dialog is complete
                                CreateDelegateResponse();
                                return response;
                            }
                       }

                       if (!ProcessDialogRequest(input, response))
                       {
                           response.Response.OutputSpeech = ProcessIntentRequest(input);
                       }
                    }
                }
                return response;
            }
            catch (Exception ex)
            {
            }
            return null; 
        }

       
        private void ProcessLaunchRequest(ResponseBody response)
        {
                IOutputSpeech innerResponse = new SsmlOutputSpeech();
                (innerResponse as SsmlOutputSpeech).Ssml = SsmlDecorate("Invoice skill has been launched.  Ask help for information on creating invoices.");
                response.OutputSpeech = innerResponse;
                IOutputSpeech prompt = new PlainTextOutputSpeech();
                (prompt as PlainTextOutputSpeech).Text = "Invoice skill has been launched.  Ask help for information on creating invoices.";
                response.Reprompt = new Reprompt()
                {
                    OutputSpeech = prompt
                };
        }
        private bool IsDialogIntentRequest(SkillRequest input)
        {
            if (string.IsNullOrEmpty(input.Request.DialogState))
                return false;
            return true;
        }

        private bool IsDialogSequenceComplete(SkillRequest input)
        {
            if (input.Request.DialogState.Equals(AlexaConstants.DialogStarted)
               || input.Request.DialogState.Equals(AlexaConstants.DialogInProgress))
            { 
                return false ;
            }
            else
            {
                if (input.Request.DialogState.Equals(AlexaConstants.DialogCompleted))
                {
                    return true;
                }
            }
            return false;
        }
        public InvoiceObject getCustomer(string customer)
        {

            WebRequest req = HttpWebRequest.Create("https://webshockinnovations.com/invoiceapi/api.php?name=" + customer);
            WebResponse res = req.GetResponse();
            StreamReader reader = new StreamReader(res.GetResponseStream());

            string json = reader.ReadToEnd();

            List<InvoiceObject> invoices = Newtonsoft.Json.JsonConvert.DeserializeObject<List<InvoiceObject>>(json);
            InvoiceObject invoice = invoices[0];
            return invoice;
        }

        public QuoteObject getInvoice(string subject)
        {

            WebRequest req = HttpWebRequest.Create("https://webshockinnovations.com/invoiceapi/api.php?getinvoice=true&subject="+subject);
            WebResponse res = req.GetResponse();
            StreamReader reader = new StreamReader(res.GetResponseStream());

            string json = reader.ReadToEnd();

            List<QuoteObject> invoices = Newtonsoft.Json.JsonConvert.DeserializeObject<List<QuoteObject>>(json);
            QuoteObject invoice = invoices[0];
            return invoice;
        }
        public CompanyObject getCompany(string companyid)
        {

            WebRequest req = HttpWebRequest.Create("https://webshockinnovations.com/invoiceapi/api.php?name=" + companyid);
            WebResponse res = req.GetResponse();
            StreamReader reader = new StreamReader(res.GetResponseStream());

            string json = reader.ReadToEnd();
            CompanyObject company = Newtonsoft.Json.JsonConvert.DeserializeObject<CompanyObject>(json); 
            return company;
        }
        private bool ProcessDialogRequest(SkillRequest input, SkillResponse response)
        {
            var intentRequest = input.Request;
            string speech_message = string.Empty;
            bool processed = false;

            switch (intentRequest.Intent.Name)
            {

                case "GetClientInvoice":
                    speech_message = GetClientInfo(intentRequest);
                    WebRequest req = HttpWebRequest.Create("https://webshockinnovations.com/invoiceapi/api.php?name=" + speech_message);
                    WebResponse res = req.GetResponse();
                    StreamReader reader = new StreamReader(res.GetResponseStream());

                    string json = reader.ReadToEnd();

                    List<InvoiceObject> invoices = Newtonsoft.Json.JsonConvert.DeserializeObject<List<InvoiceObject>>(json);
                    InvoiceObject invoice = invoices[0];
                    speech_message = "There are " + invoices.Count.ToString() + " invoices " + invoice.customer_name + " at company " + invoice.customer_company_name;

                    if (!string.IsNullOrEmpty(speech_message))
                    {
                        response.Response.OutputSpeech = new SsmlOutputSpeech();
                        (response.Response.OutputSpeech as SsmlOutputSpeech).Ssml = SsmlDecorate(speech_message);
                    }
                    processed = true;
                    break;

                case "Help":
                    speech_message = "With this skill you can add customers, companies, and create invoices.  Say customer help, company help, and invoice help to learn how to do each.";
                    if (!string.IsNullOrEmpty(speech_message))
                    {
                        response.Response.OutputSpeech = new SsmlOutputSpeech();
                        (response.Response.OutputSpeech as SsmlOutputSpeech).Ssml = SsmlDecorate(speech_message);
                    }
                    processed = true;
                    break;
                case "CustomerHelp":
                    speech_message = "Say get customers to see all customers.  To add a new customer, say New customer {customer_name} with email {customer_email} and company {customer_company_name} with phone {customer_phone_number} and address {customer_street_address}";
                    if (!string.IsNullOrEmpty(speech_message))
                    {
                        response.Response.OutputSpeech = new SsmlOutputSpeech();
                        (response.Response.OutputSpeech as SsmlOutputSpeech).Ssml = SsmlDecorate(speech_message);
                    }
                    processed = true;
                    break;
                case "CompanyHelp":
                    speech_message = "Say get companies to see all companies. To add a new company, say new company {company_name} with email {email} and phone {phone_number} with address {street_address}";
                    if (!string.IsNullOrEmpty(speech_message))
                    {
                        response.Response.OutputSpeech = new SsmlOutputSpeech();
                        (response.Response.OutputSpeech as SsmlOutputSpeech).Ssml = SsmlDecorate(speech_message);
                    }
                    processed = true;
                    break;
                case "InvoiceHelp":
                    speech_message = "Say get invoice of {customer} to get invoices for a customer.  To create an invoice say Create invoice for {customer} with subject {subject} and description {description}.  To add a product to an invoice say add {quantity} of {product} at {price} to {subject}";

                    if (!string.IsNullOrEmpty(speech_message))
                    {
                        response.Response.OutputSpeech = new SsmlOutputSpeech();
                        (response.Response.OutputSpeech as SsmlOutputSpeech).Ssml = SsmlDecorate(speech_message);
                    }
                    processed = true;
                    break;


                case "GetCustomers":
                    WebRequest customerreq = HttpWebRequest.Create("https://webshockinnovations.com/invoiceapi/api.php?getcustomers=true");
                    WebResponse customerres = customerreq.GetResponse();
                    StreamReader customerreader = new StreamReader(customerres.GetResponseStream());

                    string customerjson = customerreader.ReadToEnd();

                    List<InvoiceObject> customers = Newtonsoft.Json.JsonConvert.DeserializeObject<List<InvoiceObject>>(customerjson);


                    speech_message = "There are " + customers.Count.ToString() + " customers. ";
                    foreach (InvoiceObject customern in customers)
                    {
                        speech_message += customern.customer_name + ", ";
                    }
                    if (!string.IsNullOrEmpty(speech_message))
                    {
                        response.Response.OutputSpeech = new SsmlOutputSpeech();
                        (response.Response.OutputSpeech as SsmlOutputSpeech).Ssml = SsmlDecorate(speech_message);
                    }
                    processed = true;
                    break;

                case "GetCompanies":
                    WebRequest companyreq = HttpWebRequest.Create("https://webshockinnovations.com/invoiceapi/api.php?getcompanies=true");
                    WebResponse companyres = companyreq.GetResponse();
                    StreamReader companyreader = new StreamReader(companyres.GetResponseStream());

                    string companyjson = companyreader.ReadToEnd();

                    List<CompanyObject> companies = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CompanyObject>>(companyjson);

                    
                    speech_message = "There are " + companies.Count.ToString() + " companies. ";
                    foreach (CompanyObject companyn in companies)
                    {
                        speech_message += companyn.company_name + ", ";
                    }
                    if (!string.IsNullOrEmpty(speech_message))
                    {
                        response.Response.OutputSpeech = new SsmlOutputSpeech();
                        (response.Response.OutputSpeech as SsmlOutputSpeech).Ssml = SsmlDecorate(speech_message);
                    }
                    processed = true;
                    break;

                case "AddClient":
                    speech_message = AddClient(intentRequest);
                    string customer = speech_message;
                    string company_id = "8";

                    InvoiceObject customerdata = getCustomer(customer);
                    string urlAddress = "https://webshockinnovations.com/invoiceapi/api.php";
                    Slot departslot;
                    string subject = "";
                    string description = "";
                    if (intentRequest.Intent.Slots.TryGetValue("subject", out departslot))
                    {
                        subject = departslot.Value.ToString();
                    }
                    if (intentRequest.Intent.Slots.TryGetValue("description", out departslot))
                    {
                        description = departslot.Value.ToString();
                    }
                    using (WebClient client = new WebClient())
                    {
                        NameValueCollection postData = new NameValueCollection()
                        {
                                { "subject", subject },
                                { "description", description },
                                { "customer_id", customerdata.id },
                                { "company_id", company_id },
                        };
                        speech_message = "Creating invoice for " + speech_message + " with subject " + subject;
                        // client.UploadValues returns page's source as byte array (byte[])
                        // so it must be transformed into a string
                        string pagesource = Encoding.UTF8.GetString(client.UploadValues(urlAddress, postData));
                    }



                    if (!string.IsNullOrEmpty(speech_message))
                    {
                        response.Response.OutputSpeech = new SsmlOutputSpeech();
                        (response.Response.OutputSpeech as SsmlOutputSpeech).Ssml = SsmlDecorate(speech_message);
                    }
                    processed = true;
                    break;
                case "AddCustomer":
                    speech_message = "what";

                    string addcustomerurl= "https://webshockinnovations.com/invoiceapi/api.php";
                    Slot addcustomerslot;
                    string customer_company_name = "";
                    string customer_email = "";
                    string customer_name = "";
                    string customer_phone_number = "";
                    string customer_street_address = "";
                    if (intentRequest.Intent.Slots.TryGetValue("customer_company_name", out addcustomerslot))
                    {
                        customer_company_name = addcustomerslot.Value.ToString();
                    }
                    if (intentRequest.Intent.Slots.TryGetValue("customer_email", out addcustomerslot))
                    {
                        customer_email = addcustomerslot.Value.ToString();
                    }
                     
                    if (intentRequest.Intent.Slots.TryGetValue("customer_name", out addcustomerslot))
                    {
                        customer_name = addcustomerslot.Value.ToString();
                    }
                    if (intentRequest.Intent.Slots.TryGetValue("customer_phone_number", out addcustomerslot))
                    {
                        customer_phone_number = addcustomerslot.Value.ToString();
                    }
                    if (intentRequest.Intent.Slots.TryGetValue("customer_street_address", out addcustomerslot))
                    {
                        customer_street_address = addcustomerslot.Value.ToString();
                    }
                    using (WebClient client = new WebClient())
                    {
                        NameValueCollection postData = new NameValueCollection()
                        {
                            { "add_customer", "true" },
                                { "customer_company_name", customer_company_name },
                                { "customer_email", customer_email }, 
                                { "customer_name", customer_name },
                                { "customer_phone_number", customer_phone_number },
                                { "customer_street_address", customer_street_address },
                        };
                        speech_message = "Adding customer " + customer_name;
                        // client.UploadValues returns page's source as byte array (byte[])
                        // so it must be transformed into a string
                        string pagesource = Encoding.UTF8.GetString(client.UploadValues(addcustomerurl, postData));
                    }
                    


                    if (!string.IsNullOrEmpty(speech_message))
                    {
                        response.Response.OutputSpeech = new SsmlOutputSpeech();
                        (response.Response.OutputSpeech as SsmlOutputSpeech).Ssml = SsmlDecorate(speech_message);
                    }
                    processed = true;
                    break;

                case "AddCompany":  
                    string addcompanyurl = "https://webshockinnovations.com/invoiceapi/api.php";
                    Slot addcompanyslot;
                    string  company_name = "";
                    string email = ""; 
                    string phone_number = "";
                    string street_address = "";
                    if (intentRequest.Intent.Slots.TryGetValue("company_name", out addcompanyslot))
                    {
                        company_name = addcompanyslot.Value.ToString();
                    }
                    if (intentRequest.Intent.Slots.TryGetValue("email", out addcompanyslot))
                    {
                        email = addcompanyslot.Value.ToString();
                    }
                    if (intentRequest.Intent.Slots.TryGetValue("phone_number", out addcompanyslot))
                    {
                        phone_number = addcompanyslot.Value.ToString();
                    }
                    if (intentRequest.Intent.Slots.TryGetValue("street_address", out addcompanyslot))
                    {
                        street_address = addcompanyslot.Value.ToString();
                    }
                    using (WebClient client = new WebClient())
                    {
                        NameValueCollection postData = new NameValueCollection()
                        {
                            { "add_company", "true" },
                                { "company_name", company_name },
                                { "email", email },
                                { "phone_number", phone_number },
                                { "street_address", street_address },
                        };
                        speech_message = "Adding company " + company_name;
                        // client.UploadValues returns page's source as byte array (byte[])
                        // so it must be transformed into a string
                        string pagesource = Encoding.UTF8.GetString(client.UploadValues(addcompanyurl, postData));
                    }



                    if (!string.IsNullOrEmpty(speech_message))
                    {
                        response.Response.OutputSpeech = new SsmlOutputSpeech();
                        (response.Response.OutputSpeech as SsmlOutputSpeech).Ssml = SsmlDecorate(speech_message);
                    }
                    processed = true;
                    break;

                case "AddProduct":
                    speech_message = AddProduct(intentRequest);
                    string invoice_subject = speech_message;

                    QuoteObject quote = getInvoice(invoice_subject);
                    string url = "https://webshockinnovations.com/invoiceapi/api.php";
                    Slot slot;
                    string product = "";
                    string quantity = "";
                    string price = "";
                    if (intentRequest.Intent.Slots.TryGetValue("product", out slot))
                    {
                        product = slot.Value.ToString();
                    }
                    if (intentRequest.Intent.Slots.TryGetValue("quantity", out slot))
                    {
                        quantity = slot.Value.ToString();
                    }
                    if (intentRequest.Intent.Slots.TryGetValue("price", out slot))
                    {
                        price = slot.Value.ToString();
                    }
                    using (WebClient client = new WebClient())
                    {
                        NameValueCollection postData = new NameValueCollection()
                        {
                                 {"add_invoice", "true" },
                                { "product", product },
                                { "quantity", quantity },
                                { "price", price },
                                { "quote_id", quote.id },
                        };
                        speech_message = "Adding " + product + " to invoice " + invoice_subject; ;
                        // client.UploadValues returns page's source as byte array (byte[])
                        // so it must be transformed into a string
                        string pagesource = Encoding.UTF8.GetString(client.UploadValues(url, postData));
                    }



                    if (!string.IsNullOrEmpty(speech_message))
                    {
                        response.Response.OutputSpeech = new SsmlOutputSpeech();
                        (response.Response.OutputSpeech as SsmlOutputSpeech).Ssml = SsmlDecorate(speech_message);
                    }
                    processed = true;
                    break;


            }


            return processed;
        }
        private string SsmlDecorate(string speech)
        {
            return "<speak>" + speech + "</speak>";
        }

        private IOutputSpeech ProcessIntentRequest(SkillRequest input)
        {
            var intentRequest = input.Request;
            IOutputSpeech innerResponse = new PlainTextOutputSpeech();
            
            switch (intentRequest.Intent.Name)
            {
               
                case AlexaConstants.CancelIntent:
                    (innerResponse as PlainTextOutputSpeech).Text = "";
                    response.Response.ShouldEndSession = true;
                    break;

                case AlexaConstants.StopIntent:
                    (innerResponse as PlainTextOutputSpeech).Text = "";
                    response.Response.ShouldEndSession = true;                    
                    break;

                case AlexaConstants.HelpIntent:
                    (innerResponse as PlainTextOutputSpeech).Text = "";
                    break;



                default:
                    (innerResponse as PlainTextOutputSpeech).Text = ""; 
                    break;
            }
            if (innerResponse.Type == AlexaConstants.SSMLSpeech)
            {
               
                (innerResponse as SsmlOutputSpeech).Ssml = "";
            }  
            return innerResponse;
        }


        private string GetClientInfo(Request request)
        {
            string speech_message = string.Empty;

            Slot departslot;
            if (request.Intent.Slots.TryGetValue("customer", out departslot))
            {
                speech_message = departslot.Value.ToString();
            }

            return speech_message;
        }

        private string AddClient(Request request)
        {
            string speech_message = string.Empty;

            Slot departslot;
            if (request.Intent.Slots.TryGetValue("customer", out departslot))
            {
                speech_message = departslot.Value.ToString();
            }

            return speech_message;
        }

        private string AddProduct(Request request)
        {
            string speech_message = string.Empty;

            Slot departslot;
            if (request.Intent.Slots.TryGetValue("subject", out departslot))
            {
                speech_message = departslot.Value.ToString();
            }

            return speech_message;
        }


        private void CreateDelegateResponse()
        {
            DialogDirective dld = new DialogDirective()
            {
                Type = AlexaConstants.DialogDelegate
            };
            response.Response.Directives.Add(dld);
        }

    }
}

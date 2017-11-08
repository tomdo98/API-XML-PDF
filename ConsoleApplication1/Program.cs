    using System;
    using System.Net;
	using System.Text;
	using System.IO;
    using System.Xml;
    using System.Data.SqlClient;
//using System.Diagnostics;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Drawing;
using PdfSharp.Pdf.Advanced;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Collections;
using common;

    class firegetAPInvoices
    {

        static void Main()
        {
            string connectionString = null;
            string PostURL = null;
            string PDFPathOut = null;
            string logXML = null;
            string XMLLogDirectory = null;
            string errorlogpath = null;
            string UseFlatFile = null;
            string threshholdxml = null;

            Guid guidsession;
            Guid documentguid;
            guidsession = Guid.NewGuid();
            documentguid = Guid.NewGuid();

            Config config;
            config = new Config();
            connectionString = config.ConnectionString;
            PostURL = config.RequestURL;
            PDFPathOut = config.PDFPath;
            logXML = config.LogXML;
            XMLLogDirectory = config.XMLLogDirectory;
            errorlogpath = config.ErrorLogDirectory + DateTime.Now.ToString("MMddyyyyhhmiss") + ".txt";
            UseFlatFile = config.UseFlatFile;
            threshholdxml = config.Threshhold;

            if (config.PullXML == "Y")
            {
                Post_Xml(connectionString, PostURL, logXML, XMLLogDirectory, errorlogpath, UseFlatFile);
            }
            if (config.CreatePDF == "Y")
            {
                PullPdf(connectionString, PDFPathOut, errorlogpath, threshholdxml);
            }
            if (config.TaxRulesConstruction == "Y")
            {
                tax_rules_construction(connectionString);
            }
            if (config.TaxRulesExpense == "Y")
            {
                tax_rules_expense(connectionString);
            }
           //Console.ReadKey();
        }

        public static void PullPdf(string Constring, string PDFPath2, string errorlogpath, string thresholdxml) 
        {
            SqlCommand command;
            string sql = null;
            SqlDataReader dataReader;
            SqlConnection cnn;

            cnn = new SqlConnection(Constring);

            try
            {
                cnn.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Can not open connection ! ");
                System.IO.File.WriteAllText(errorlogpath, "Can not open connection !");
            }


            string guidsessionpdf = null;
            string guiddocumentpdf = null;
            string uniqueident = null;
            string PDFPath = null;
            PDFPath = PDFPath2;

            // select documents that need a pdf
            command = new SqlCommand("select session_guid, document_guid, business_unit + '_' + invoice_id from prestage_invoice_esb where pdf_created = 'N'", cnn);
            dataReader = command.ExecuteReader();
            while (dataReader.Read())
            {
                guidsessionpdf += dataReader.GetValue(0);
                guiddocumentpdf += dataReader.GetValue(1);
                uniqueident += dataReader.GetValue(2);
                //Console.WriteLine(dataReader.GetValue(0) + " - " + dataReader.GetValue(1) + " - " + dataReader.GetValue(2));

                createVPDF(PDFPath + guiddocumentpdf + "_" + uniqueident + ".pdf", guidsessionpdf, guiddocumentpdf, Constring, errorlogpath, thresholdxml);
                guidsessionpdf = null;
                guiddocumentpdf = null;
                uniqueident = null;
            }
            dataReader.Close();
            command.Dispose();

            cnn.Close();
        }

        public static void Post_Xml(string ConString, string PostToURL, string LogXML, string LogXMLDirectory, string ErrorLogPath, string UseFlatFile)
        {
            Guid guidsession;
            Guid documentguid;
            guidsession = Guid.NewGuid();
            documentguid = Guid.NewGuid();
            string responseString="";
            
            //var request = (HttpWebRequest)WebRequest.Create("https://*");
            var request = (HttpWebRequest)WebRequest.Create(PostToURL);
            var loadGetAPFile = new System.IO.StreamReader("GetAPInvoices.xml", System.Text.Encoding.UTF8);
            var GetAPInvoices = loadGetAPFile.ReadToEnd();
            var data = Encoding.ASCII.GetBytes(GetAPInvoices);
            
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;

            if (UseFlatFile == "N")
            {
                try
                {
                    using (var stream = request.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("error posting XML");
                    System.IO.File.WriteAllText(ErrorLogPath, "Error Posting XML");
                }

                var response = (HttpWebResponse)request.GetResponse();
                //var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                Console.WriteLine(responseString);

                if (LogXML == "Y")
                {
                    System.IO.File.WriteAllText(LogXMLDirectory + DateTime.Now.ToString("MMddyyyyhhmiss") + ".xml", responseString);
                }
            }
           

            //Begin Database Connection
            SqlCommand command;
            string sql = null;
            SqlDataReader dataReader;
            SqlConnection cnn;
            cnn = new SqlConnection(ConString);

            try
            {
                cnn.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Can not open connection ! ");
            }

            string invoice_number = null;
            string business_unit = null;
            string attribute = null;
            string attribute2 = null;
            string vendor_id = null;
            string currency_cd = null;
            string invoice_date = null;
            string client_account_code = null;
            DateTime invoice_date_dt = DateTime.MinValue;
            string country = null;
            string gross_invoice_amount = "0.00";
            string net_invoice_amount = "0.00";
            string tax = "0.00";
            DateTime date_created_dt = DateTime.MinValue;
            string date_created;
            string total = "0.00";
            string taxline = "0.00";
            string taxrate = "0.00";
            string price = "0.00";
            string quantity = "0.00";
            string descr = "";
            string part_number = "";
            string rate = "0.00";
            string hours = "0.00";
            string uom = "";
            int line = 0;
            string work_order_number = "";
            string prov_name = "";
            string prov_description = "";
            string approver = "";
            string approver_login = "";
            string record_count = "0.00";
            string invoices_total = "0.00";
            string capital_expense = "";
            string site_description = "";
            //string threshhold = "0.00";

            decimal price_p;
            decimal quantity_p;
            decimal total_p;



            XmlReader reader;
            reader = XmlReader.Create(new StringReader(responseString));      
            if (UseFlatFile == "N")
            {
                reader = XmlReader.Create(new StringReader(responseString));                
            }
            else
            {
                //reader = XmlReader.Create("prodsample.xml");
                reader = XmlReader.Create("manual_load.xml");
                System.IO.File.Copy(System.IO.Directory.GetCurrentDirectory() +"\\manual_load.xml", LogXMLDirectory +  "\\manual_load.xml", true);
            }

            using (reader)
            {
                // Parse the file and display each of the nodes.
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            // writer.WriteStartElement(reader.Name);
                            Console.WriteLine(reader.Name);
                            Console.WriteLine(reader.Value);
                            if (reader.HasAttributes)
                            {
                                Console.WriteLine("Attributes of <" + reader.Name + ">");
                                attribute = reader.Name;
                                while (reader.MoveToNextAttribute())
                                {
                                    Console.WriteLine(" {0}={1}", reader.Name, reader.Value);
                                    if (attribute == "inv_footer")
                                    {
                                        if (reader.Name == "record_count")
                                        {
                                            record_count = reader.Value;
                                        }
                                        if (reader.Name == "invoices_total")
                                        {
                                            invoices_total = reader.Value;
                                        }
                                    }
                                    if (attribute == "labor")
                                    {
                                        if (reader.Name == "total")
                                        {
                                            total = reader.Value;
                                        }
                                        if (reader.Name == "tax")
                                        {
                                            taxline = reader.Value;
                                        }
                                    }
                                    if (attribute == "travel")
                                    {
                                        if (reader.Name == "total")
                                        {
                                            total = reader.Value;
                                        }
                                        if (reader.Name == "tax")
                                        {
                                            taxline = reader.Value;
                                        }
                                    }
                                    if (attribute == "travel_charge")
                                    {
                                        if (reader.Name == "total")
                                        {
                                            total = reader.Value;
                                        }
                                        if (reader.Name == "taxrate")
                                        {
                                            taxrate = reader.Value;
                                        }
                                        if (reader.Name == "price")
                                        {
                                            price = reader.Value;
                                        }
                                        if (reader.Name == "quantity")
                                        {
                                            quantity = reader.Value;
                                        }
                                        if (reader.Name == "description")
                                        {
                                            descr = reader.Value;
                                        }
                                    }
                                    if (attribute == "misc_charge")
                                    {
                                        if (reader.Name == "total")
                                        {
                                            total = reader.Value;
                                        }
                                        if (reader.Name == "taxrate")
                                        {
                                            taxrate = reader.Value;
                                        }
                                        if (reader.Name == "price")
                                        {
                                            price = reader.Value;
                                        }
                                        if (reader.Name == "quantity")
                                        {
                                            quantity = reader.Value;
                                        }
                                        if (reader.Name == "description")
                                        {
                                            descr = reader.Value;
                                           
                                        }
                                    }
                                    if (attribute == "labor_charge")
                                    {
                                        if (reader.Name == "rate")
                                        {
                                            price = reader.Value;
                                        }
                                        if (reader.Name == "technician")
                                        {
                                            descr = reader.Value;
                                        }
                                        if (reader.Name == "hours")
                                        {
                                            quantity = reader.Value;
                                        }
                                        if (reader.Name == "total")
                                        {
                                            total = reader.Value;
                                        }
                                        if (reader.Name == "taxrate")
                                        {
                                            taxrate = reader.Value;
                                        }
                                    }
                                    if (attribute == "labor_adj")
                                    {
                                        if (reader.Name == "rate")
                                        {
                                            price = reader.Value;
                                        }
                                        if (reader.Name == "description")
                                        {
                                            descr = reader.Value;
                                        }
                                        if (reader.Name == "hours")
                                        {
                                            quantity = reader.Value;
                                        }
                                        if (reader.Name == "total")
                                        {
                                            total = reader.Value;
                                        }
                                        if (reader.Name == "tax")
                                        {
                                            taxrate = reader.Value;
                                        }
                                    }
                                    if (attribute == "parts")
                                    {
                                        if (reader.Name == "total")
                                        {
                                            total = reader.Value;
                                        }
                                        if (reader.Name == "tax")
                                        {
                                            taxline = reader.Value;
                                        }
                                    }
                                    if (attribute == "misc")
                                    {
                                        if (reader.Name == "total")
                                        {
                                            total = reader.Value;
                                        }
                                        if (reader.Name == "tax")
                                        {
                                            taxline = reader.Value;
                                        }
                                    }
                                    if (attribute == "part")
                                    {
                                        if (reader.Name == "name")
                                        {
                                            descr = reader.Value;
                                        }
                                        if (reader.Name == "number")
                                        {
                                            part_number = reader.Value;
                                        }
                                        if (reader.Name == "uom")
                                        {
                                            uom = reader.Value;
                                        }
                                        if (reader.Name == "price")
                                        {
                                            price = reader.Value;
                                        }
                                        if (reader.Name == "quantity")
                                        {
                                            quantity = reader.Value;
                                        }
                                        if (reader.Name == "tax")
                                        {
                                            taxline = reader.Value;
                                        }
                                        if (reader.Name == "taxrate")
                                        {
                                            taxrate = reader.Value;
                                        }
                                    }
                                    if (attribute == "inv")
                                    {
                                        if (reader.Name == "capital_expense")
                                        {
                                            capital_expense = reader.Value;
                                        }
                                        if (reader.Name == "site_description")
                                        {
                                            site_description = reader.Value;
                                        }
                                        if (reader.Name == "prov_invoice_number")
                                        {
                                            invoice_number = reader.Value;
                                        }
                                        if (reader.Name == "prov_description")
                                        {
                                            prov_description = reader.Value;
                                        }

                                        if (reader.Name == "prov_name")
                                        {
                                            prov_name = reader.Value;
                                        }
                                        if (reader.Name == "work_order_number")
                                        {
                                            work_order_number = reader.Value;
                                        }

                                        if (reader.Name == "approver")
                                        {
                                            approver = reader.Value;
                                        }
                                        if (reader.Name == "approver_login")
                                        {
                                            approver_login = reader.Value;
                                        }


                                        if (reader.Name == "site_name")
                                        {
                                            business_unit = reader.Value;
                                        }
                                        if (reader.Name == "client_account_code")
                                        {
                                            client_account_code = reader.Value;
                                        }
                                        if (reader.Name == "prov_vendor_number")
                                        {
                                            vendor_id = reader.Value;
                                        }
                                        if (reader.Name == "prov_currency_code")
                                        {
                                            currency_cd = reader.Value;
                                        }
                                        if (reader.Name == "wo_invoice_date")
                                        {
                                            invoice_date = reader.Value;
                                            invoice_date_dt = DateTime.ParseExact(invoice_date, "MMddyyyy", System.Globalization.CultureInfo.InvariantCulture);

                                        }
                                        if (reader.Name == "prov_country_code")
                                        {
                                            country = reader.Value;
                                        }
                                        if (reader.Name == "gross_invoice_amount")
                                        {
                                            gross_invoice_amount = reader.Value;
                                        }
                                        if (reader.Name == "net_invoice_amount")
                                        {
                                            net_invoice_amount = reader.Value;
                                        }
                                        if (reader.Name == "tax")
                                        {
                                            tax = reader.Value;
                                        }
                                        if (reader.Name == "date_created")
                                        {
                                            date_created = reader.Value;
                                            date_created_dt = DateTime.ParseExact(date_created, "MMddyyyy", System.Globalization.CultureInfo.InvariantCulture);

                                        }
                                    }
                                }
                                if (attribute == "inv_footer")
                                {
                                    command = new SqlCommand();
                                    command.Connection = cnn;
                                    command.CommandText = "insert into prestage_xml_total values (@param1,@param2,@param3,getdate())";
                                    command.Parameters.AddWithValue("@param1", guidsession);
                                    command.Parameters.AddWithValue("@param2", record_count);
                                    command.Parameters.AddWithValue("@param3", invoices_total);
                                    command.ExecuteNonQuery();
                                    command.Dispose();
                                }
                                if (attribute == "inv")
                                {
                                    try
                                    {
                                        line = 0;
                                        command = new SqlCommand();
                                        command.Connection = cnn;
                                        documentguid = Guid.NewGuid();
                                        //command.CommandText = "insert into prestage_invoice_esb values (@param1,@param2,@param3,@param4,'',getdate(),@param6,@param7,@param8,@param9,@param10,@param11,@param12,@param13,'N','','N','',@param14,@param15,@param16,0,'',0,0,0,0,'','','','','','','','','','','','','','','','',0,0,0,0,'Construction','',@param17,'','*','VER',@param18,@param19,'',0,0,0,'','Construction_Single','N/A','','',@param20)";
                                        command.CommandText = "insert into prestage_invoice_esb values (@param1,@param2,@param3,@param4,'',getdate(),@param6,@param7,@param8,@param9,@param10,@param11,@param12,@param13,'N','','N','',@param14,@param15,@param16,0,'',0,0,0,0,'','','','','','','','','','','','','','',@param21,'',0,0,0,0,'Construction','',@param17,'','*','VER',@param18,@param19,'',0,0,0,'','Construction_Single','N/A','','',@param20)";
                                        command.Parameters.AddWithValue("@param1", guidsession);
                                        command.Parameters.AddWithValue("@param2", documentguid);
                                        //for testing purposes hard coding 
                                        //business_unit = "BRK - 10006";
                                        if (business_unit.Length>5)
                                        {
                                            business_unit = business_unit.Substring(business_unit.Length - 5);
                                        }
                                        command.Parameters.AddWithValue("@param3",business_unit );
                                        command.Parameters.AddWithValue("@param4", invoice_number);
                                        command.Parameters.AddWithValue("@param6", vendor_id);
                                        command.Parameters.AddWithValue("@param7", currency_cd);
                                        command.Parameters.AddWithValue("@param8", invoice_date_dt);
                                        command.Parameters.AddWithValue("@param9", country);
                                        command.Parameters.AddWithValue("@param10", gross_invoice_amount);
                                        command.Parameters.AddWithValue("@param11", net_invoice_amount);
                                        command.Parameters.AddWithValue("@param12", tax);
                                        command.Parameters.AddWithValue("@param13", date_created_dt);
                                        command.Parameters.AddWithValue("@param14", work_order_number);
                                        command.Parameters.AddWithValue("@param15", prov_name);
                                        command.Parameters.AddWithValue("@param16", prov_description);                                        
                                        command.Parameters.AddWithValue("@param17", client_account_code);
                                        command.Parameters.AddWithValue("@param18", approver);
                                        command.Parameters.AddWithValue("@param19", approver_login);
                                        command.Parameters.AddWithValue("@param20", capital_expense);
                                        command.Parameters.AddWithValue("@param21", site_description);
                                

                                        command.ExecuteNonQuery();
                                        command.Dispose();


                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                        Console.ReadKey();
                                    }
                                }

                                if (attribute == "parts" || attribute == "part" || attribute == "labor" || attribute == "labor_charge" || attribute == "misc" || attribute == "travel" || attribute == "travel_charge" || attribute == "misc" || attribute == "misc_charge" || attribute == "labor_adj" )
                                {
                                    try
                                    {
                                         if ((descr.StartsWith("Freight", System.StringComparison.CurrentCultureIgnoreCase)) || (descr.StartsWith("Shipping", System.StringComparison.CurrentCultureIgnoreCase)))
                                         {
                                             attribute = "freight";

                                             price_p = decimal.Parse(price);
                                             quantity_p = decimal.Parse(quantity);
                                             total_p = price_p * quantity_p;
                                             total = total_p.ToString();
                                         }
                                        line++;
                                        command = new SqlCommand();
                                        command.Connection = cnn;
                                        //documentguid = Guid.NewGuid();
                                        command.CommandText = "insert into prestage_invoice_line_esb values (@param1,@param2," + line + ",@param4,@param5,@param6,@param7,@param8,@param9,@param10,@param11,@param12,@param13,@param14,getdate())";
                                        command.Parameters.AddWithValue("@param1", guidsession);
                                        command.Parameters.AddWithValue("@param2", documentguid);
                                        command.Parameters.AddWithValue("@param4", attribute);
                                        command.Parameters.AddWithValue("@param5", total);
                                        command.Parameters.AddWithValue("@param6", taxline);
                                        command.Parameters.AddWithValue("@param7", taxrate);
                                        command.Parameters.AddWithValue("@param8", price);
                                        command.Parameters.AddWithValue("@param9", quantity);
                                        command.Parameters.AddWithValue("@param10", descr);
                                        command.Parameters.AddWithValue("@param11", part_number);
                                        command.Parameters.AddWithValue("@param12", rate);
                                        command.Parameters.AddWithValue("@param13", hours);
                                        command.Parameters.AddWithValue("@param14", uom);
                                        command.ExecuteNonQuery();
                                        command.Dispose();
                                        attribute = "";
                                        total = "0.00";
                                        taxline = "0.00";
                                        taxrate = "0.00";
                                        price = "0.00";
                                        quantity = "0.00";
                                        descr = "";
                                        part_number = "";
                                        rate = "0.00";
                                        hours = "0.00";
                                        uom = "";

                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                        Console.ReadKey();
                                    }
                                }
                                // Move the reader back to the element node.
                                reader.MoveToElement();
                            }
                            break;

                        case XmlNodeType.Text:
                            //writer.WriteString(reader.Value);
                            Console.WriteLine(reader.Value);
                            break;
                        case XmlNodeType.XmlDeclaration:
                        case XmlNodeType.ProcessingInstruction:
                            //writer.WriteProcessingInstruction(reader.Name, reader.Value);
                            Console.WriteLine(reader.Name);
                            Console.WriteLine(reader.Value);
                            break;
                        case XmlNodeType.Comment:
                            //writer.WriteComment(reader.Value);
                            Console.WriteLine(reader.Value);
                            break;
                        case XmlNodeType.EndElement:
                            //writer.WriteFullEndElement();
                            break;
                    }
                }


            }

        }

     


   //sql2 = "update prestage_invoice_esb set tax_reporting = '" + tax_reporting + "'  where session_guid ='" + guidsesh + "' and document_guid = '" + guiddoc + "'";
   
   command2 = new SqlCommand(sql2, cnn2);
   command2.CommandText = "update prestage_invoice_esb set tax_reporting = '" + tax_reporting +"'  where session_guid ='" + guidsesh + "' and document_guid = '" + guiddoc + "'";
   command2.ExecuteNonQuery();
   command2.Dispose();

                }
                dataReader.Close();
                command.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            cnn.Close();
            cnn2.Close();
        }


        public static void createVPDF(string pdffilename, string guidsesh, string guiddoc, string ConString, string errorlogpath, string thresholdxml)
        {
            PdfDocument pdf = new PdfDocument();
            pdf.Info.Title = "";
            PdfPage pdfPage = pdf.AddPage();
            XGraphics graph = XGraphics.FromPdfPage(pdfPage);
            XFont font = new XFont("Verdana", 8, XFontStyle.Bold);
            XFont fontregular = new XFont("Verdana", 6, XFontStyle.Regular);
            XFont fontunderline = new XFont("Verdana", 6, XFontStyle.Underline);
            XFont fontsmallbold = new XFont("Verdana", 6, XFontStyle.Bold);
            int lineposition = 10;
            decimal traveltotal = 0;
            decimal labortotal = 0;
            //decimal partstotal = 0;
            decimal installation = 0;

           // XGraphics gfx = XGraphics.FromPdfPage(pdfPage);
            //horizontal lines
            graph.DrawLine(XPens.Black, 3, 6, 380, 6);
            graph.DrawLine(XPens.Black, 3, 19, 380, 19);
            graph.DrawLine(XPens.Black, 3, 80, 380, 80);
            //vertical lines
            graph.DrawLine(XPens.Black, 3, 6, 3, 80);
            graph.DrawLine(XPens.Black, 190, 6, 190, 80);
            //horizontal lines
            //graph.DrawLine(XPens.Black, 190, 3, 380, 3);
            //graph.DrawLine(XPens.Black, 190, 80, 380, 80);
            //vertical lines
            graph.DrawLine(XPens.Black, 380, 6, 380, 80);


            string connectionString = null;
            //SqlConnection connection; 
            SqlCommand command;
            string sql = null;
            SqlDataReader dataReader;
            SqlConnection cnn;
            string pdfline = null;
            string pdfheader = null;
            decimal gross_invoice_amount = 0;
            decimal net_amount = 0;
            decimal tax_total = 0;
            decimal misc_total = 0;
            string shiptocurrency = "";
            string shiptoname = "";
            string shipto_address1 = "";
            string shipto_address2= "";
            string shipto_address3= "";
            string shipto_address4= "";
            string shipto_city= "";
            string shipto_state= "";
            string shipto_postal= "";
            string tax_pct = "";
            string business_unit = "";
            string vendor_id = "";
            string address1 = "";
            string address2 = "";
            string address3 = "";
            string address4 = "";
            string city = "";
            string state = "";
            string postal = "";
            string suppliername = "";
            string attribute = "";
            string supplieraccount = "";
            string terms = "";
            decimal laborheadertotal = 0;
            decimal partsheadertotal = 0;
            decimal travelheadertotal = 0;
            decimal mischeadertotal = 0;
            decimal subtotalheader = 0;
            decimal freightheader = 0;
            

            //connectionString = "Server=FINSYS-DB-DEV\\FINSYSDEV;Database=WFM_Basware_Prod;Trusted_Connection=False;User ID=bwread;Password =wh0lef00ds";
            //connectionString = "Server=FINSYS-DB-TST\\FINSYSTST;Database=WFM_Basware_Prod;Trusted_Connection=False;User ID=bwadmin;Password =bw@dm1ntest";
            connectionString = ConString;
            cnn = new SqlConnection(connectionString);
      
            try
            {
                cnn.Open();
                

                sql = "select a.work_order_number, a.provider_name, a.provider_description, a.invoice_id, a.invoice_date, a.tax_total, a.net_amount, a.gross_invoice_amount, a.vendor_id, a.business_unit,a.freight from prestage_invoice_esb a where a.session_guid ='" + guidsesh + "' and a.document_guid = '" + guiddoc + "'";
                command = new SqlCommand(sql, cnn);
                dataReader = command.ExecuteReader();
                while (dataReader.Read())
                {
                    tax_total = dataReader.GetDecimal(5);
                    net_amount = dataReader.GetDecimal(6);
                    gross_invoice_amount = dataReader.GetDecimal(7);
                    business_unit = dataReader.GetString(9);
                    vendor_id = dataReader.GetString(8);
                    freightheader = dataReader.GetDecimal(10);
                }
                dataReader.Close();
                command.Dispose();


                command.CommandText = "update prestage_invoice_esb set exception_code = 'Missing Data'  where session_guid ='" + guidsesh + "' and document_guid = '" + guiddoc + "' and (invoice_date = '' or invoice_id = '' or gross_invoice_amount = 0)";
                    command.ExecuteNonQuery();
                    command.Dispose();


                // update table with ship to address from BU
                sql = "select shipto_name, shipto_address1, shipto_address2, shipto_address3, shipto_address4,shipto_city, shipto_state, shipto_zip, tax_pct, shipto_currency from xx_shipto_address where shipto_id = '" + business_unit + "'";
                command = new SqlCommand(sql, cnn);
                dataReader = command.ExecuteReader();
                dataReader.Read();
                //dataReader.GetString(0);
                if (dataReader.HasRows)
                {
                    shiptoname = dataReader.GetString(0);
                    shipto_address1 = dataReader.GetString(1);
                    shipto_address2 = dataReader.GetString(2);
                    shipto_address3 = dataReader.GetString(3);
                    shipto_address4 = dataReader.GetString(4);
                    shipto_city = dataReader.GetString(5);
                    shipto_state = dataReader.GetString(6);
                    shipto_postal = dataReader.GetString(7);
                    tax_pct = dataReader.GetString(8);
                    shiptocurrency = dataReader.GetString(9);
                    dataReader.Close();
                    command.Dispose();

                    command.CommandText = "update prestage_invoice_esb set ship_to_address1 = '" + shipto_address1 + "', ship_to_address2 = '" + shipto_address2 + "', ship_to_address3 = '" + shipto_address3 + "',ship_to_address4 = '" + shipto_address4 + "',ship_to_city = '" + shipto_city + "',ship_to_state = '" + shipto_state + "',ship_to_postal = '" + shipto_postal + "', bu_descr = '" + shiptoname + "', ps_tax_rate = " + tax_pct + ",threshhold_pct=" + thresholdxml + ",shipto_currency='" + shiptocurrency + "',default_gl_account='" + supplieraccount + "',terms='" + terms + "'  where session_guid ='" + guidsesh + "' and document_guid = '" + guiddoc + "'";
                    command.ExecuteNonQuery();
                    command.Dispose();
                }
                else
                {
                    dataReader.Close();
                    command.Dispose();

                    //command.CommandText = "update prestage_invoice_esb set business_unit = '99999', bu_descr = 'Please validate ShipTo Address', tax_reporting = 'Tax Not Found'  where session_guid ='" + guidsesh + "' and document_guid = '" + guiddoc + "'";
                    command.CommandText = "update prestage_invoice_esb set business_unit = '99999', tax_reporting = 'Tax Not Found'  where session_guid ='" + guidsesh + "' and document_guid = '" + guiddoc + "'";
                    command.ExecuteNonQuery();
                    command.Dispose();
                }

                // update table with remit details
                // update table with ship to address from BU
                //business_unit = business_unit.Substring(business_unit.Length - 5);
                //sql = "select supplier_name, supplier_7, supplier_8, supplier_9, ' ', supplier_10, supplier_11, supplier_12, supplier_account_1 from supplier where substring(supplier_num,1,10) ='" + vendor_id + "'";
                sql = "select supplier_name, isnull(supplier_5,''), isnull(supplier_6,''), isnull(supplier_7,''), isnull(supplier_8,''), isnull(supplier_10,''), isnull(supplier_11,''), isnull(supplier_12,''), isnull(supplier_account_1,''), isnull(supplier_2,'') from supplier where supplier_num ='" + vendor_id + "'";
                command = new SqlCommand(sql, cnn);
                dataReader = command.ExecuteReader();
                dataReader.Read();
                if (dataReader.HasRows)
                {
                    //dataReader.GetString(0);
                    suppliername = dataReader.GetString(0);
                    address1 = dataReader.GetString(1);
                    address2 = dataReader.GetString(2);
                    address3 = dataReader.GetString(3);
                    address4 = dataReader.GetString(4);
                    city = dataReader.GetString(5);
                    state = dataReader.GetString(6);
                    postal = dataReader.GetString(7);
                    supplieraccount = dataReader.GetString(8);
                    terms = dataReader.GetString(9);
                    dataReader.Close();
                    command.Dispose();

                    command.CommandText = "update prestage_invoice_esb set remit_to_address1 = '" + address1 + "', remit_to_address2 = '" + address2 + "', remit_to_address3 = '" + address3 + "',remit_to_address4 = '" + address4 + "',remit_to_city = '" + city + "',remit_to_state = '" + state + "',remit_to_postal = '" + postal + "', supplier_name = '" + suppliername + "',default_gl_account='" + supplieraccount + "',terms='" + terms + "' where session_guid ='" + guidsesh + "' and document_guid = '" + guiddoc + "'";
                    command.ExecuteNonQuery();
                    command.Dispose();
                }
                else
                {
                    dataReader.Close();
                    command.Dispose();

                    //command.CommandText = "update prestage_invoice_esb set vendor_id ='9999999999', supplier_name = 'INVALID VENDOR', exception_code = 'Vendor Maintenance' where session_guid ='" + guidsesh + "' and document_guid = '" + guiddoc + "'";
                    command.CommandText = "update prestage_invoice_esb set vendor_id ='9999999999',  exception_code = 'Vendor Maintenance' where session_guid ='" + guidsesh + "' and document_guid = '" + guiddoc + "'";
                    command.ExecuteNonQuery();
                    command.Dispose();
                }
                // update header totals
                sql = "select attribute, sum(total) from prestage_invoice_line_esb b where attribute in ('parts','labor','travel','misc','freight') and session_guid ='" + guidsesh + "' and document_guid = '" + guiddoc + "' group by attribute";
                command = new SqlCommand(sql, cnn);
                dataReader = command.ExecuteReader();
                while (dataReader.Read())
                {
                    attribute = dataReader.GetString(0);
                    if (attribute == "labor")
                    {
                        laborheadertotal = dataReader.GetDecimal(1);
                    }
                    if (attribute == "parts")
                    {
                        partsheadertotal = dataReader.GetDecimal(1);
                    }
                    if (attribute == "misc")
                    {
                        mischeadertotal = dataReader.GetDecimal(1);
                    }
                    if (attribute == "travel")
                    {
                        travelheadertotal = dataReader.GetDecimal(1);
                    }
                    if (attribute == "freight")
                    {
                        freightheader = dataReader.GetDecimal(1);
                    }
                }
                mischeadertotal = mischeadertotal - freightheader;
                installation = laborheadertotal + travelheadertotal;
                subtotalheader = gross_invoice_amount - installation - freightheader - tax_total;
                dataReader.Close();
                command.Dispose();

                command.CommandText = "update prestage_invoice_esb set installation= " + installation  + ", freight = " + freightheader + ", labor_total=" + laborheadertotal + " , travel_total = " + travelheadertotal + ", misc_total = " + mischeadertotal + ", parts_total = " + partsheadertotal + ", subtotal = " + subtotalheader +"  where session_guid ='" + guidsesh + "' and document_guid = '" + guiddoc + "'";
                command.ExecuteNonQuery();
                command.Dispose();

                sql = "select business_unit, invoice_id, vendor_id, currency_cd, convert(char,invoice_date,101), country, gross_invoice_amount, tax_total, " +
                    "work_order_number, provider_name, provider_description, installation, freight, subtotal, labor_total, remit_to_address1," +
                    "remit_to_address2, remit_to_address3, remit_to_address4, remit_to_city,remit_to_state, remit_to_postal, ship_to_address1, ship_to_address2," +
                    "ship_to_address3, ship_to_address4,ship_to_city, ship_to_state, ship_to_postal, bu_descr, supplier_name, travel_total, " +
                    "misc_total, parts_total from prestage_invoice_esb a where a.session_guid ='" + guidsesh + "' and a.document_guid = '" + guiddoc + "'";

                command = new SqlCommand(sql, cnn);
                dataReader = command.ExecuteReader();
                while (dataReader.Read())
                {
                    //Console.WriteLine(dataReader.GetValue(0) + " - " + dataReader.GetValue(1) + " - " + dataReader.GetValue(2));
                    graph.DrawString("Ship To", fontsmallbold, XBrushes.Black, new XRect(10, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    lineposition += 10;
                    graph.DrawString(dataReader.GetString(29) + " - " + dataReader.GetString(0), fontregular, XBrushes.Black, new XRect(10, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    lineposition += 10;
                    graph.DrawString(dataReader.GetString(22) , fontregular, XBrushes.Black, new XRect(10, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    lineposition += 10;
                    if (dataReader.GetString(23) != "")
                    {
                        graph.DrawString(dataReader.GetString(23), fontregular, XBrushes.Black, new XRect(10, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                        lineposition += 10;
                    }
                    if (dataReader.GetString(24) != "")
                    {
                        graph.DrawString(dataReader.GetString(24), fontregular, XBrushes.Black, new XRect(10, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                        lineposition += 10;
                    }
                    if (dataReader.GetString(25) != "")
                    {
                        graph.DrawString(dataReader.GetString(25), fontregular, XBrushes.Black, new XRect(10, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                        lineposition += 10;
                    }
                    graph.DrawString(dataReader.GetString(26) + ", " + dataReader.GetString(27) + " " + dataReader.GetString(28), fontregular, XBrushes.Black, new XRect(10, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    lineposition = 10;
                    graph.DrawString("Vendor", fontsmallbold, XBrushes.Black, new XRect(200, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    lineposition += 10;
                    graph.DrawString(dataReader.GetString(30) + " - " + dataReader.GetString(1), fontregular, XBrushes.Black, new XRect(200, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    lineposition += 10;
                    graph.DrawString(dataReader.GetString(15), fontregular, XBrushes.Black, new XRect(200, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    lineposition += 10;
                    if (dataReader.GetString(16) != "")
                    {
                        graph.DrawString(dataReader.GetString(16), fontregular, XBrushes.Black, new XRect(200, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                        lineposition += 10;
                    }
                    if (dataReader.GetString(17) != "")
                    {
                        graph.DrawString(dataReader.GetString(17), fontregular, XBrushes.Black, new XRect(200, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                        lineposition += 10;
                    }
                    if (dataReader.GetString(18) != "")
                    {
                        graph.DrawString(dataReader.GetString(18), fontregular, XBrushes.Black, new XRect(200, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                        lineposition += 10;
                    }
                    graph.DrawString(dataReader.GetString(19) + ", " + dataReader.GetString(20) + " " + dataReader.GetString(21), fontregular, XBrushes.Black, new XRect(200, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    lineposition = 10;
                    // invoice number. right blow
                    graph.DrawString("Invoice Number", fontsmallbold, XBrushes.Black, new XRect(400, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    graph.DrawString("Work Order", fontsmallbold, XBrushes.Black, new XRect(500, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    lineposition += 10;
                    graph.DrawString(dataReader.GetString(1), fontregular, XBrushes.Black, new XRect(400, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    graph.DrawString(dataReader.GetString(8), fontregular, XBrushes.Black, new XRect(500, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    lineposition += 10;
                    graph.DrawString("Invoice Date", fontsmallbold, XBrushes.Black, new XRect(400, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    graph.DrawString("Invoice Currency", fontsmallbold, XBrushes.Black, new XRect(500, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    lineposition += 10;
                    graph.DrawString(dataReader.GetString(4), fontregular, XBrushes.Black, new XRect(400, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    graph.DrawString(dataReader.GetString(3), fontregular, XBrushes.Black, new XRect(500, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    lineposition += 10;
                }
                dataReader.Close();
                command.Dispose();

                sql = "select descr, quantity, price, rtrim(ltrim(str(price*quantity,10,2))) from prestage_invoice_line_esb a where attribute = 'travel_charge' and a.session_guid ='" + guidsesh + "' and a.document_guid = '" + guiddoc + "'";
                command = new SqlCommand(sql, cnn);
                dataReader = command.ExecuteReader();

                lineposition = 100;
                graph.DrawString("Travel", fontsmallbold, XBrushes.Black, new XRect(10, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                graph.DrawString("Amount", fontsmallbold, XBrushes.Black, new XRect(250, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);

                while (dataReader.Read())
                {
                    lineposition += 10;   
                    pdfheader = dataReader.GetValue(0) + " Qty: " + dataReader.GetValue(1) + " Price: " + dataReader.GetValue(2);
                    graph.DrawString(pdfheader, fontregular, XBrushes.Black, new XRect(10, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    pdfheader = dataReader.GetValue(3) + " ";
                    graph.DrawString(pdfheader, fontregular, XBrushes.Black, new XRect(250, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);                    
                }
                
                dataReader.Close();
                command.Dispose();
                lineposition += 10;
                graph.DrawString("Subtotal", fontsmallbold, XBrushes.Black, new XRect(250, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                graph.DrawString("Tax", fontsmallbold, XBrushes.Black, new XRect(300, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                graph.DrawString("Total", fontsmallbold, XBrushes.Black, new XRect(350, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);

                sql = "select total, tax, total+tax from prestage_invoice_line_esb a where attribute = 'travel' and a.session_guid ='" + guidsesh + "' and a.document_guid = '" + guiddoc + "'";
                command = new SqlCommand(sql, cnn);
                dataReader = command.ExecuteReader();
                lineposition += 10;
                while (dataReader.Read())
                {
                    //subtotal
                    graph.DrawString(dataReader.GetValue(0) + "", fontregular, XBrushes.Black, new XRect(250, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    graph.DrawString(dataReader.GetValue(1) + "", fontregular, XBrushes.Black, new XRect(300, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    graph.DrawString(dataReader.GetValue(2) + "", fontregular, XBrushes.Black, new XRect(350, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    lineposition += 10;
                }

                dataReader.Close();
                command.Dispose();



                //sql = "select descr, price, quantity, rtrim(ltrim(str(price*quantity,10,2))) from prestage_invoice_line_esb a where attribute = 'labor_charge' and a.session_guid ='" + guidsesh + "' and a.document_guid = '" + guiddoc + "'";
                //sql = "select descr, quantity,price,  total from prestage_invoice_line_esb a where attribute = 'labor_charge' and a.session_guid ='" + guidsesh + "' and a.document_guid = '" + guiddoc + "'";
                sql = "select descr, quantity,price,  total from prestage_invoice_line_esb a where attribute in ('labor_charge','labor_adj') and a.session_guid ='" + guidsesh + "' and a.document_guid = '" + guiddoc + "'";
                command = new SqlCommand(sql, cnn);
                dataReader = command.ExecuteReader();

                lineposition += 20;
                //Console.WriteLine(dataReader.GetValue(0) + " - " + dataReader.GetValue(1) + " - " + dataReader.GetValue(2));
                graph.DrawString("Labor", fontsmallbold, XBrushes.Black, new XRect(10, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                graph.DrawString("Amount", fontsmallbold, XBrushes.Black, new XRect(250, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                lineposition += 10;

                while (dataReader.Read())
                {
                    pdfheader = dataReader.GetValue(0) + " Hours: " + dataReader.GetValue(1) + " Price: " + dataReader.GetValue(2);
                    graph.DrawString(pdfheader, fontregular, XBrushes.Black, new XRect(10, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    pdfheader = dataReader.GetValue(3) + " ";
                    graph.DrawString(pdfheader, fontregular, XBrushes.Black, new XRect(250, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    lineposition += 10;
                }

                dataReader.Close();
                command.Dispose();

                graph.DrawString("Subtotal", fontsmallbold, XBrushes.Black, new XRect(250, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                graph.DrawString("Tax", fontsmallbold, XBrushes.Black, new XRect(300, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                graph.DrawString("Total", fontsmallbold, XBrushes.Black, new XRect(350, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                lineposition += 10;

                sql = "select total, tax, total+tax from prestage_invoice_line_esb a where attribute = 'labor' and a.session_guid ='" + guidsesh + "' and a.document_guid = '" + guiddoc + "'";
                command = new SqlCommand(sql, cnn);
                dataReader = command.ExecuteReader();
                while (dataReader.Read())
                {
                    //subtotal labor
                    graph.DrawString(dataReader.GetValue(0) + "", fontregular, XBrushes.Black, new XRect(250, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    graph.DrawString(dataReader.GetValue(1) + "", fontregular, XBrushes.Black, new XRect(300, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    graph.DrawString(dataReader.GetValue(2) + "", fontregular, XBrushes.Black, new XRect(350, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    lineposition += 10;
                }

                dataReader.Close();
                command.Dispose();

                sql = "select descr, quantity, price,  rtrim(ltrim(str(price*quantity,10,2))) from prestage_invoice_line_esb a where attribute = 'part' and a.session_guid ='" + guidsesh + "' and a.document_guid = '" + guiddoc + "'";
                command = new SqlCommand(sql, cnn);
                dataReader = command.ExecuteReader();

                lineposition += 20;
                //Console.WriteLine(dataReader.GetValue(0) + " - " + dataReader.GetValue(1) + " - " + dataReader.GetValue(2));
                graph.DrawString("Parts", fontsmallbold, XBrushes.Black, new XRect(10, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                graph.DrawString("Amount", fontsmallbold, XBrushes.Black, new XRect(250, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                lineposition += 10;

                while (dataReader.Read())
                {
                    pdfheader = dataReader.GetValue(0) + " Qty: " + dataReader.GetValue(1) + " Price: " + dataReader.GetValue(2);
                    graph.DrawString(pdfheader, fontregular, XBrushes.Black, new XRect(10, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    pdfheader = dataReader.GetValue(3) + " ";
                    graph.DrawString(pdfheader, fontregular, XBrushes.Black, new XRect(250, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    lineposition += 10;
                }

                dataReader.Close();
                command.Dispose();
                graph.DrawString("Subtotal", fontsmallbold, XBrushes.Black, new XRect(250, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                graph.DrawString("Tax", fontsmallbold, XBrushes.Black, new XRect(300, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                graph.DrawString("Total", fontsmallbold, XBrushes.Black, new XRect(350, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                lineposition += 10;

                sql = "select total, tax, total+tax from prestage_invoice_line_esb a where attribute = 'parts' and a.session_guid ='" + guidsesh + "' and a.document_guid = '" + guiddoc + "'";
                command = new SqlCommand(sql, cnn);
                dataReader = command.ExecuteReader();
                while (dataReader.Read())
                {
                    //subtotal parts
                    graph.DrawString(dataReader.GetValue(0) + "", fontregular, XBrushes.Black, new XRect(250, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    graph.DrawString(dataReader.GetValue(1) + "", fontregular, XBrushes.Black, new XRect(300, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    graph.DrawString(dataReader.GetValue(2) + "", fontregular, XBrushes.Black, new XRect(350, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    lineposition += 10;
                }

                dataReader.Close();
                command.Dispose();

                sql = "select descr, price, quantity, rtrim(ltrim(str(price*quantity,10,2))) from prestage_invoice_line_esb a where attribute in ( 'misc_charge','freight') and a.session_guid ='" + guidsesh + "' and a.document_guid = '" + guiddoc + "'";
                command = new SqlCommand(sql, cnn);
                dataReader = command.ExecuteReader();

                lineposition += 20;
                //Console.WriteLine(dataReader.GetValue(0) + " - " + dataReader.GetValue(1) + " - " + dataReader.GetValue(2));
                graph.DrawString("Misc", fontsmallbold, XBrushes.Black, new XRect(10, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                graph.DrawString("Amount", fontsmallbold, XBrushes.Black, new XRect(250, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
  
                lineposition += 10;

                while (dataReader.Read())
                {
                    pdfheader = dataReader.GetValue(0) + " Qty: " + dataReader.GetValue(1) + " Price: " + dataReader.GetValue(2);
                    graph.DrawString(pdfheader, fontregular, XBrushes.Black, new XRect(10, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    pdfheader = dataReader.GetValue(3) + " ";
                    graph.DrawString(pdfheader, fontregular, XBrushes.Black, new XRect(250, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    lineposition += 10;
                }

                dataReader.Close();
                command.Dispose();
                graph.DrawString("Subtotal", fontsmallbold, XBrushes.Black, new XRect(250, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                graph.DrawString("Tax", fontsmallbold, XBrushes.Black, new XRect(300, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                graph.DrawString("Total", fontsmallbold, XBrushes.Black, new XRect(350, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                lineposition += 10;

                sql = "select total, tax, total+ tax from prestage_invoice_line_esb a where attribute = 'misc' and a.session_guid ='" + guidsesh + "' and a.document_guid = '" + guiddoc + "'";
                command = new SqlCommand(sql, cnn);
                dataReader = command.ExecuteReader();
                while (dataReader.Read())
                {
                    graph.DrawString(dataReader.GetValue(0) + "", fontregular, XBrushes.Black, new XRect(250, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    graph.DrawString(dataReader.GetValue(1) + "", fontregular, XBrushes.Black, new XRect(300, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    graph.DrawString(dataReader.GetValue(2) + "", fontregular, XBrushes.Black, new XRect(350, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                    lineposition += 10;
                }

                dataReader.Close();
                command.Dispose();



                command.CommandText = "update prestage_invoice_esb set pdf_created = 'Y', file_name ='" + pdffilename + "', date_pdf_created = getdate()  where session_guid ='" + guidsesh + "' and document_guid = '" + guiddoc + "'";
                command.ExecuteNonQuery();
                command.Dispose();
                lineposition += 10;
                graph.DrawString("Summary", fontsmallbold, XBrushes.Black, new XRect(100, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                graph.DrawString("Amount", fontsmallbold, XBrushes.Black, new XRect(250, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                lineposition += 10;
                graph.DrawString("Parts & Misc", fontregular, XBrushes.Black, new XRect(100, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                graph.DrawString(partsheadertotal + mischeadertotal + "", fontregular, XBrushes.Black, new XRect(250, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                lineposition += 10;
                graph.DrawString("Freight", fontregular, XBrushes.Black, new XRect(100, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                graph.DrawString(freightheader + "", fontregular, XBrushes.Black, new XRect(250, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                lineposition += 10;
                graph.DrawString("Install, Labor, & Travel ", fontregular, XBrushes.Black, new XRect(100, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                graph.DrawString(installation + "", fontregular, XBrushes.Black, new XRect(250, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                lineposition += 10;
                graph.DrawString("Tax ", fontregular, XBrushes.Black, new XRect(100, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                graph.DrawString(tax_total + "", fontregular, XBrushes.Black, new XRect(250, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                lineposition += 10;
                graph.DrawString("Total", fontregular, XBrushes.Black, new XRect(100, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                graph.DrawString(gross_invoice_amount + "", fontregular, XBrushes.Black, new XRect(250, lineposition, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);

                cnn.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                System.IO.File.AppendAllText(errorlogpath , ex.Message + "\n");
            }

            string pdfFilename = pdffilename;
            pdf.Save(pdfFilename);

        }



    } 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
//using System.Diagnostics;

namespace common
{
    public class Config
    {
        private string _ConnectionString;
        private string _PDFPath;
        private string _RequestURL;
        private string _LogXML;
        private string _XMLLogDirectory;
        private string _ErrorLogDirectory;
        private string _PullXML;
        private string _CreatePDF;
        private string _TaxRulesConstruction;
        private string _TaxRulesExpense;
        private string _UseFlatFile;
        private string _Threshhold;


        public string UseFlatFile
        {
            get { return _UseFlatFile; }
            private set { _UseFlatFile = value; }
        }
        public string PullXML
        {
            get { return _PullXML; }
            private set { _PullXML = value; }
        }
        public string CreatePDF
        {
            get { return _CreatePDF; }
            private set { _CreatePDF = value; }
        }
        public string TaxRulesConstruction
        {
            get { return _TaxRulesConstruction; }
            private set { _TaxRulesConstruction = value; }
        }
        public string TaxRulesExpense
        {
            get { return _TaxRulesExpense; }
            private set { _TaxRulesExpense = value; }
        }
        public string ConnectionString
        {
            get { return _ConnectionString; }
            private set { _ConnectionString = value; }
        }
        public string PDFPath
        {
            get { return _PDFPath; }
            private set { _PDFPath = value; }
        }
        public string RequestURL
        {
            get { return _RequestURL; }
            private set { _RequestURL = value; }
        }

        public string LogXML
        {
            get { return _LogXML; }
            private set { _LogXML = value; }
        }
        public string XMLLogDirectory
        {
            get { return _XMLLogDirectory; }
            private set { _XMLLogDirectory = value; }
        }

        public string ErrorLogDirectory
        {
            get { return _ErrorLogDirectory; }
            private set { _ErrorLogDirectory = value; }
        }

        public string Threshhold
        {
            get { return _Threshhold; }
            private set { _Threshhold = value; }
        }

        public Config()
        {
            ConnectionString = ConfigurationManager.AppSettings["ConnectionString"];
            PDFPath = ConfigurationManager.AppSettings["PDFPath"];
            RequestURL = ConfigurationManager.AppSettings["RequestURL"];
            LogXML= ConfigurationManager.AppSettings["LogXML"];
            XMLLogDirectory= ConfigurationManager.AppSettings["XMLLogDirectory"];
            ErrorLogDirectory= ConfigurationManager.AppSettings["ErrorLogDirectory"];
            PullXML = ConfigurationManager.AppSettings["PullXML"]; 
            CreatePDF = ConfigurationManager.AppSettings["CreatePDF"]; 
            TaxRulesConstruction = ConfigurationManager.AppSettings["TaxRulesConstruction"]; 
            TaxRulesExpense = ConfigurationManager.AppSettings["TaxRulesExpense"];
            UseFlatFile = ConfigurationManager.AppSettings["UseFlatFile"];
            Threshhold = ConfigurationManager.AppSettings["Threshhold"];
        }
    }
}

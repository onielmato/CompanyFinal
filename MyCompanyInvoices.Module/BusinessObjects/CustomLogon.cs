using System;
using System.Linq;
using System.Text;
using DevExpress.Xpo;
using DevExpress.ExpressApp;
using System.ComponentModel;
using DevExpress.ExpressApp.DC;
using DevExpress.Data.Filtering;
using DevExpress.Persistent.Base;
using System.Collections.Generic;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Persistent.Validation;
using System.Runtime.Serialization;
using DevExpress.Xpo.DB.Helpers;
using DevExpress.ExpressApp.Security;

namespace MyCompanyInvoices.Module.BusinessObjects
{
    public interface IDatabaseNameParameter
    {
        string DatabaseName { get; set; }
    }
    [DomainComponent]
    [System.ComponentModel.DisplayName("Log In")]
    [DefaultClassOptions]
    //[ImageName("BO_Contact")]
    //[DefaultProperty("DisplayMemberNameForLookupEditorsOfThisType")]
    //[DefaultListViewOptions(MasterDetailMode.ListViewOnly, false, NewItemRowPosition.None)]
    //[Persistent("DatabaseTableName")]
    // Specify more UI options using a declarative approach (https://documentation.devexpress.com/#eXpressAppFramework/CustomDocument112701).
    public class CustomLogon : AuthenticationStandardLogonParameters, IDatabaseNameParameter
    { // Inherit from a different class to provide a custom primary key, concurrency and deletion behavior, etc. (https://documentation.devexpress.com/eXpressAppFramework/CustomDocument113146.aspx).

        public CustomLogon() { }
        // ISerializable  
        public CustomLogon(SerializationInfo info, StreamingContext context)
        {
            if (info.MemberCount > 0)
            {
                UserName = info.GetString("UserName");
                Password = info.GetString("Password");
            }
            else
            {
                UserName = "admin";
                Password = string.Empty;
            }
        }
        //public CustomLogon(Session session)
        //    : base(session)
        //{
        //}
        //public override void AfterConstruction()
        //{
        //    base.AfterConstruction();
        //    // Place your initialization code here (https://documentation.devexpress.com/eXpressAppFramework/CustomDocument112834.aspx).
        //}



        CompanySeller companySeller;
        Subsidiary subsidiary;
        Company company;
        private string password;

        private string databaseName = ChangeDatabaseHelper.Databases.Split(';')[0];
        //[ModelDefault("PredefinedValues", ChangeDatabaseHelper.Databases)]
        public string DatabaseName
        {
            get { return databaseName; }
            set { databaseName = value; }
        }

        [ImmediatePostData]
        public Company Company
        {
            get
            {
                return company;
            }
            set
            {
                if (value == company) return;
                company = value;
                CompanySeller = null;
                OnPropertyChanged("Company");

            }
        }


        [DataSourceProperty("Company.Subsidiaries"), ImmediatePostData]
        public Subsidiary Subsidiary
        {
            get
            {
                return subsidiary;
            }
            set
            {
                if (value == subsidiary) return;
                subsidiary = value;
                // CompanySeller = null;              
                OnPropertyChanged("Subsidiary");
            }
        }
        [DataSourceProperty("Subsidiary.Sellers"), ImmediatePostData]
        public CompanySeller CompanySeller
        {
            get
            {
                return companySeller;
            }
            set
            {
                if (Subsidiary == null) return;
                companySeller = value;
                if (companySeller != null)
                {
                    UserName = companySeller.UserName;
                }
                OnPropertyChanged("CompanySeller");
            }
        }
        [Browsable(false)]
        public new String UserName { get; set; }
        [PasswordPropertyText(true)]
        public new string Password
        {
            get { return password; }
            set
            {
                if (password == value) return;
                password = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChangedMe;

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChangedMe == null)
            {
                return;
            }

            PropertyChangedMe(this, new PropertyChangedEventArgs(propertyName));
        }
        [System.Security.SecurityCritical]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("UserName", UserName);
            info.AddValue("Password", Password);
        }

    }

    public class ChangeDatabaseHelper
    {
         public const string Databases = "MyCompanyInvoices;MyCompanyInvoices1";
        public static void UpdateDatabaseName(XafApplication application, string databaseName)
        {
            ConnectionStringParser helper = new ConnectionStringParser(application.ConnectionString);
            var conexion = helper.GetConnectionString();
            //if MSSqlServer 
            if (conexion.Contains("MSSqlServer"))
            {
                helper.RemovePartByName("Data Source");
                application.ConnectionString = string.Format("Data Source={0};{1}", databaseName, helper.GetConnectionString());
            }//if MysqlServer
            else if (conexion.Contains("MySql"))
            {
                helper.RemovePartByName("Server");
                application.ConnectionString = string.Format("Server={0};{1}", databaseName, helper.GetConnectionString());
            }//SQL Server
            else if (conexion.Contains("Postgres"))
            {
                helper.RemovePartByName("Server");
                application.ConnectionString = string.Format("Server={0};{1}", databaseName, helper.GetConnectionString());
            }




        }
    }




}
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

namespace MyCompanyInvoices.Module.BusinessObjects
{
   [NonPersistent()]
    public class TipoDePago : BaseObject
    { // Inherit from a different class to provide a custom primary key, concurrency and deletion behavior, etc. (https://documentation.devexpress.com/eXpressAppFramework/CustomDocument113146.aspx).
        public TipoDePago(Session session)
            : base(session)
        {
        }
        public override void AfterConstruction()
        {
            base.AfterConstruction();
            // Place your initialization code here (https://documentation.devexpress.com/eXpressAppFramework/CustomDocument112834.aspx).
        }

        string invoiceCode;
        double totalPagar;
        double payCash;

        [Size(SizeAttribute.DefaultStringMappingFieldSize)]
        [ModelDefault("AllowEdit", "False")]
        public string InvoiceCode
        {
            get => invoiceCode;
            set => SetPropertyValue(nameof(InvoiceCode), ref invoiceCode, value);
        }

        private PayType? payType;
        [ImmediatePostData]
        public PayType? PayType
        {
            get => payType;
            set => SetPropertyValue(nameof(PayType), ref payType, value);
        }
        [ModelDefault("AllowEdit", "False")]
        public double TotalPagar
        {
            get => totalPagar;
            set => SetPropertyValue(nameof(TotalPagar), ref totalPagar, value);
        }

        public double PayCash
        {
            get => payCash;
            set => SetPropertyValue(nameof(PayCash), ref payCash, value);
        }
            

        
       
    }

    public enum PayType
    {
        pagoParcial = 1,
        pagoTotal = 2
    }
}
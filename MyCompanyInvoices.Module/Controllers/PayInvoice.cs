using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Layout;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Templates;
using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using MyCompanyInvoices.Module.BusinessObjects;
using DevExpress.XtraEditors;
using AuthorizeNet.Api.Controllers.Bases;
using AuthorizeNet.Api.Contracts.V1;
using AuthorizeNet.Api.Controllers;
using System.Windows.Forms;
using DevExpress.Utils;

namespace MyCompanyInvoices.Module.Controllers
{
    // For more typical usage scenarios, be sure to check out https://documentation.devexpress.com/eXpressAppFramework/clsDevExpressExpressAppViewControllertopic.aspx.
    public partial class PayInvoice : ViewController
    {
        PopupWindowShowAction popupWindowShowAction;
        public PayInvoice()
        {
            TargetObjectType = typeof(Invoice);
            popupWindowShowAction = new PopupWindowShowAction(this, "Tipo de Pago", PredefinedCategory.Edit);
            popupWindowShowAction.ImageName = "BO_Sale";
            popupWindowShowAction.CustomizePopupWindowParams += PopupWindowShowAction_CustomizePopupWindowParams;
                                                                                                                                                                                                                          
           
        }


        DetailView detailView;
        TipoDePago tipoDePago;
        private void PopupWindowShowAction_CustomizePopupWindowParams(object sender, CustomizePopupWindowParamsEventArgs e)
        {
            IObjectSpace objectSpace = Application.CreateObjectSpace();
            detailView = Application.CreateDetailView(objectSpace, objectSpace.CreateObject<TipoDePago>());
            detailView.ViewEditMode = ViewEditMode.Edit;
            e.View = detailView;
            
            var os = this.Application.CreateObjectSpace();
            tipoDePago = os.CreateObject<TipoDePago>();
            tipoDePago = (TipoDePago)detailView.CurrentObject;
            foreach (Invoice item in View.SelectedObjects)
            {
                tipoDePago.TotalPagar = item.CurrentInvoiceTotal;
                tipoDePago.InvoiceCode = item.Code;
                //    //objectSpace.CommitChanges();
            }
            if (os.IsModified)
            {
                os.CommitChanges();
            }


            e.Action.Execute += Action_Execute;
        }

        private void Action_Execute(object sender, PopupWindowShowActionExecuteEventArgs e)
        {
           IList<Invoice> invoiceSelects = this.View.SelectedObjects.Cast<Invoice>().ToList();   
           foreach (Invoice task in invoiceSelects)
           {
              if (tipoDePago.PayType == PayType.pagoTotal)
                {
                    ApiOperationBase<ANetApiRequest, ANetApiResponse>.RunEnvironment = AuthorizeNet.Environment.SANDBOX;
                    ApiOperationBase<ANetApiRequest, ANetApiResponse>.MerchantAuthentication = new merchantAuthenticationType()
                    {
                        name = task.Seller.ApiLoginID,
                        ItemElementName = ItemChoiceType.transactionKey,
                        Item = task.Seller.TransactionKey,
                    };
                    var creditCard = new creditCardType
                    {
                        cardNumber = task.Client.CredirCard,
                        expirationDate = task.Client.ExpirationDate,
                        cardCode = task.Client.CardCode
                    };
                    var billingAddress = new customerAddressType
                    {
                        firstName = task.Client.Name,
                        lastName = task.Client.LastName,
                        address = task.Client.UserAddress.Street,
                        city = task.Client.UserAddress.City,
                        state = task.Client.UserAddress.StateProvince,
                        company = task.Seller.Company.CompanyName,
                        phoneNumber = task.Client.PhoneNumber,
                        zip = task.Client.UserAddress.ZipPostal
                    };

                    var paymentType = new paymentType { Item = creditCard };

                    int cantProducts = task.Products.Count;
                    var lineItems = new lineItemType[cantProducts];

                    int i = 0;
                    foreach (var product in task.Products)
                    {
                        lineItems[i] = new lineItemType { itemId = i.ToString(), name = product.Name, quantity = 1, unitPrice = new Decimal(product.Price) };
                        i++;
                    }

                    var orderNumber = new orderType
                    {
                        invoiceNumber = task.Code,
                        description = "NULL"
                    };

                    var transactionRequest = new transactionRequestType
                    {
                        transactionType = transactionTypeEnum.authOnlyTransaction.ToString(),    // charge the card

                        amount = new Decimal(task.CurrentInvoiceTotal),
                        payment = paymentType,
                        billTo = billingAddress,
                        lineItems = lineItems,
                        order = orderNumber

                    };

                    var request = new createTransactionRequest { transactionRequest = transactionRequest };

                    // instantiate the controller that will call the service
                    var controller = new createTransactionController(request);
                    controller.Execute();

                    // get the response from the service (errors contained if any)
                    var response = controller.GetApiResponse();

                    if (response != null)
                    {
                        if (response.messages.resultCode == messageTypeEnum.Ok)
                        {
                            if (response.transactionResponse.messages != null)
                            {
                                XtraMessageBoxArgs args = new XtraMessageBoxArgs();
                                args.Caption = "Transacción Autorizada.";
                                args.Text = "La transacción ha sido autorizada. TransID: " + response.transactionResponse.transId + "--" + "Response Code: " + response.transactionResponse.responseCode;
                                args.Buttons = new DialogResult[] { DialogResult.OK, DialogResult.Cancel };
                                args.Showing += Args_Showing;
                                if (XtraMessageBox.Show(args) == DialogResult.OK)
                                {
                                    ApiOperationBase<ANetApiRequest, ANetApiResponse>.RunEnvironment = AuthorizeNet.Environment.SANDBOX;
                                    ApiOperationBase<ANetApiRequest, ANetApiResponse>.MerchantAuthentication = new merchantAuthenticationType()
                                    {
                                        name = task.Seller.ApiLoginID,
                                        ItemElementName = ItemChoiceType.transactionKey,
                                        Item = task.Seller.TransactionKey,
                                    };

                                    var _transactionRequest = new transactionRequestType
                                    {
                                        transactionType = transactionTypeEnum.priorAuthCaptureTransaction.ToString(),    // charge the card
                                        amount = new Decimal(task.CurrentInvoiceTotal),
                                        refTransId = response.transactionResponse.transId

                                    };
                                    var requestCapture = new createTransactionRequest { transactionRequest = transactionRequest };

                                    // instantiate the controller that will call the service
                                    var controllerCapture = new createTransactionController(requestCapture);
                                    controllerCapture.Execute();

                                    // get the response from the service (errors contained if any)
                                    var responseCapture = controller.GetApiResponse();
                                    if (responseCapture != null)
                                    {
                                        if (responseCapture.messages.resultCode == messageTypeEnum.Ok)
                                        {
                                            if (responseCapture.transactionResponse.messages != null)
                                            {
                                                if (XtraMessageBox.Show("El Pago ha sido procesado exitosamente:", "Success Transaction", MessageBoxButtons.OK) == DialogResult.OK)
                                                {
                                                    task.InvoiceTotal = 0;
                                                    task.Status = Status.InProgress;
                                                    ObjectSpace.CommitChanges();
                                                    //View.Close();
                                                }

                                            }
                                            else
                                            {
                                                if (XtraMessageBox.Show("Failed Transaction: Error Code: " + response.transactionResponse.errors[0].errorCode + "--" + "Error message: " + response.transactionResponse.errors[0].errorText, "Failed Transaction.", MessageBoxButtons.OK, MessageBoxIcon.Error) == DialogResult.OK)
                                                {
                                                    task.Status = Status.InProgress;
                                                    View.Close();
                                                }

                                            }
                                        }
                                        else
                                        {
                                            if (XtraMessageBox.Show("Failed Transaction: Error Code: " + response.transactionResponse.errors[0].errorCode + "--" + "Error message: " + response.transactionResponse.errors[0].errorText, "Failed Transaction.", MessageBoxButtons.OK, MessageBoxIcon.Error) == DialogResult.OK)
                                            {
                                                task.Status = Status.InProgress;
                                                View.Close();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (XtraMessageBox.Show("El Pago no pudo ser procesado.", "Error", MessageBoxButtons.OK) == DialogResult.OK)
                                        {
                                            task.Status = Status.InProgress;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (XtraMessageBox.Show("Failed Transaction: Error Code: " + response.transactionResponse.errors[0].errorCode + "--" + "Error message: " + response.transactionResponse.errors[0].errorText, "Failed Transaction.", MessageBoxButtons.OK, MessageBoxIcon.Error) == DialogResult.OK)
                                {
                                    task.Status = Status.InProgress;
                                    View.Close();
                                }
                            }
                        }
                        else
                        {
                            if (response.transactionResponse != null && response.transactionResponse.errors != null)
                            {
                                if (XtraMessageBox.Show("Failed Transaction: Error Code: " + response.transactionResponse.errors[0].errorCode + "--" + "Error message: " + response.transactionResponse.errors[0].errorText, "Failed Transaction.", MessageBoxButtons.OK, MessageBoxIcon.Error) == DialogResult.OK)
                                {
                                    task.Status = Status.InProgress;
                                    View.Close();
                                }

                            }
                        }
                    }
                    else
                    {
                        if (XtraMessageBox.Show("Rejected Authorization.", "Rejected", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Cancel)
                        {
                            task.Status = Status.Started;
                            View.Close();
                        }


                    }
                }
                else if (tipoDePago.PayType == PayType.pagoParcial)
                {
                    ApiOperationBase<ANetApiRequest, ANetApiResponse>.RunEnvironment = AuthorizeNet.Environment.SANDBOX;
                    ApiOperationBase<ANetApiRequest, ANetApiResponse>.MerchantAuthentication = new merchantAuthenticationType()
                    {
                        name = task.Seller.ApiLoginID,
                        ItemElementName = ItemChoiceType.transactionKey,
                        Item = task.Seller.TransactionKey,
                    };
                    var creditCard = new creditCardType
                    {
                        cardNumber = task.Client.CredirCard,
                        expirationDate = task.Client.ExpirationDate,
                        cardCode = task.Client.CardCode
                    };
                    var billingAddress = new customerAddressType
                    {
                        firstName = task.Client.Name,
                        lastName = task.Client.LastName,
                        address = task.Client.UserAddress.Street,
                        city = task.Client.UserAddress.City,
                        state = task.Client.UserAddress.StateProvince,
                        company = task.Seller.Company.CompanyName,
                        phoneNumber = task.Client.PhoneNumber,
                        zip = task.Client.UserAddress.ZipPostal
                    };

                    var paymentType = new paymentType { Item = creditCard };

                    int cantProducts = task.Products.Count;
                    var lineItems = new lineItemType[cantProducts];

                    int i = 0;
                    foreach (var product in task.Products)
                    {
                        lineItems[i] = new lineItemType { itemId = i.ToString(), name = product.Name, quantity = 1, unitPrice = new Decimal(product.Price) };
                        i++;
                    }

                    var orderNumber = new orderType
                    {
                        invoiceNumber = task.Code,
                        description = "NULL"
                    };

                    var transactionRequest = new transactionRequestType
                    {
                        transactionType = transactionTypeEnum.authOnlyTransaction.ToString(),    // charge the card

                        amount = new Decimal(task.CurrentInvoiceTotal - tipoDePago.PayCash),
                        payment = paymentType,
                        billTo = billingAddress,
                        lineItems = lineItems,
                        order = orderNumber

                    };

                    var request = new createTransactionRequest { transactionRequest = transactionRequest };

                    // instantiate the controller that will call the service
                    var controller = new createTransactionController(request);
                    controller.Execute();

                    // get the response from the service (errors contained if any)
                    var response = controller.GetApiResponse();

                    if (response != null)
                    {
                        if (response.messages.resultCode == messageTypeEnum.Ok)
                        {
                            if (response.transactionResponse.messages != null)
                            {
                                XtraMessageBoxArgs args = new XtraMessageBoxArgs();
                                args.Caption = "Transacción Autorizada.";
                                args.Text = "La transacción ha sido autorizada. TransID: " + response.transactionResponse.transId + "--" + "Response Code: " + response.transactionResponse.responseCode;
                                args.Buttons = new DialogResult[] { DialogResult.OK, DialogResult.Cancel };
                                args.Showing += Args_Showing;
                                if (XtraMessageBox.Show(args) == DialogResult.OK)
                                {
                                    ApiOperationBase<ANetApiRequest, ANetApiResponse>.RunEnvironment = AuthorizeNet.Environment.SANDBOX;
                                    ApiOperationBase<ANetApiRequest, ANetApiResponse>.MerchantAuthentication = new merchantAuthenticationType()
                                    {
                                        name = task.Seller.ApiLoginID,
                                        ItemElementName = ItemChoiceType.transactionKey,
                                        Item = task.Seller.TransactionKey,
                                    };

                                    var _transactionRequest = new transactionRequestType
                                    {
                                        transactionType = transactionTypeEnum.priorAuthCaptureTransaction.ToString(),    // charge the card
                                        amount = new Decimal(task.CurrentInvoiceTotal - tipoDePago.PayCash),
                                        refTransId = response.transactionResponse.transId

                                    };
                                    var requestCapture = new createTransactionRequest { transactionRequest = transactionRequest };

                                    // instantiate the controller that will call the service
                                    var controllerCapture = new createTransactionController(requestCapture);
                                    controllerCapture.Execute();

                                    // get the response from the service (errors contained if any)
                                    var responseCapture = controller.GetApiResponse();
                                    if (responseCapture != null)
                                    {
                                        if (responseCapture.messages.resultCode == messageTypeEnum.Ok)
                                        {
                                            if (responseCapture.transactionResponse.messages != null)
                                            {
                                                if (XtraMessageBox.Show("El Pago ha sido procesado exitosamente:", "Success Transaction", MessageBoxButtons.OK) == DialogResult.OK)
                                                {
                                                    task.Pago = task.CurrentInvoiceTotal - tipoDePago.PayCash;
                                                    //task.CurrentInvoiceTotal = 45;
                                                    task.Status = Status.InProgress;
                                                    if (ObjectSpace.IsModified)
                                                        ObjectSpace.CommitChanges();
                                                    //View.Close();
                                                }

                                            }
                                            else
                                            {
                                                if (XtraMessageBox.Show("Failed Transaction: Error Code: " + response.transactionResponse.errors[0].errorCode + "--" + "Error message: " + response.transactionResponse.errors[0].errorText, "Failed Transaction.", MessageBoxButtons.OK, MessageBoxIcon.Error) == DialogResult.OK)
                                                {
                                                    task.Status = Status.InProgress;
                                                    View.Close();
                                                }

                                            }
                                        }
                                        else
                                        {
                                            if (XtraMessageBox.Show("Failed Transaction: Error Code: " + response.transactionResponse.errors[0].errorCode + "--" + "Error message: " + response.transactionResponse.errors[0].errorText, "Failed Transaction.", MessageBoxButtons.OK, MessageBoxIcon.Error) == DialogResult.OK)
                                            {
                                                task.Status = Status.InProgress;
                                                View.Close();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (XtraMessageBox.Show("El Pago no pudo ser procesado.", "Error", MessageBoxButtons.OK) == DialogResult.OK)
                                        {
                                            task.Status = Status.InProgress;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (XtraMessageBox.Show("Failed Transaction: Error Code: " + response.transactionResponse.errors[0].errorCode + "--" + "Error message: " + response.transactionResponse.errors[0].errorText, "Failed Transaction.", MessageBoxButtons.OK, MessageBoxIcon.Error) == DialogResult.OK)
                                {
                                   View.Close();
                                }
                            }
                        }
                        else
                        {
                            if (response.transactionResponse != null && response.transactionResponse.errors != null)
                            {
                                if(XtraMessageBox.Show("Failed Transaction: Error Code: " + response.transactionResponse.errors[0].errorCode + "--" + "Error message: " + response.transactionResponse.errors[0].errorText,"Failed Transaction.", MessageBoxButtons.OK, MessageBoxIcon.Error) == DialogResult.OK)
                                {
                                    View.Close();
                                }
                                    
                            }
                        }
                    }
                    else
                    {
                        if(XtraMessageBox.Show("Rejected Authorization.","Rejected",MessageBoxButtons.RetryCancel,MessageBoxIcon.Error) == DialogResult.Cancel)
                        {
                            task.Status = Status.Started;
                            View.Close();
                        }
                        

                    }
                }
            } 


        }

        private void Args_Showing(object sender, XtraMessageShowingArgs e)
        {
            SvgImageCollection collection = SvgImageCollection.FromResources("MyCompanyInvoices.Module.Images", typeof(MyCompanyInvoicesModule).Assembly);
            foreach (var item in e.Form.Controls)
            {
                SimpleButton button = item as SimpleButton;
                if (button != null)
                {
                    button.ImageOptions.SvgImageSize = new System.Drawing.Size(16, 16);
                    button.ImageOptions.SvgImage = collection[0];
                    switch (button.DialogResult.ToString())
                    {
                        case ("OK"):
                            button.ImageOptions.SvgImage = collection[3];
                            button.Text = "Aceptar";
                            break;
                        case ("Cancel"):
                            button.ImageOptions.SvgImage = collection[5];
                            break;

                    }

                }
            }
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            
            View.SelectionChanged += View_SelectionChanged;
            
        }

        

       
        private void View_SelectionChanged(object sender, EventArgs e)
        {
            IList<Invoice> invoiceSelects = this.View.SelectedObjects.Cast<Invoice>().ToList();
            foreach (Invoice item in invoiceSelects)
            {
                if (item.Status == Status.Completed)
                    popupWindowShowAction.Enabled.SetItemValue("ID", false);
                else
                    popupWindowShowAction.Enabled.SetItemValue("ID", true);
            }
        }

       protected override void OnViewControlsCreated()
        {
            base.OnViewControlsCreated();
            
        }
        protected override void OnDeactivated()
        {
            // Unsubscribe from previously subscribed events and release other references and resources.
            base.OnDeactivated();
        }
    }
}

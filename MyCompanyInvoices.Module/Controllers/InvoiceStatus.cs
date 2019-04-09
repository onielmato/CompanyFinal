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
using AuthorizeNet.Api.Controllers;
using AuthorizeNet.Api.Contracts.V1;
using AuthorizeNet.Api.Controllers.Bases;
using MyCompanyInvoices.Module.BusinessObjects;
using DevExpress.XtraEditors;
using System.Windows.Forms;
using DevExpress.Utils.Svg;
using DevExpress.Utils;
using static DevExpress.XtraPrinting.Native.ExportOptionsPropertiesNames;

namespace MyCompanyInvoices.Module.Controllers
{
    // For more typical usage scenarios, be sure to check out https://documentation.devexpress.com/eXpressAppFramework/clsDevExpressExpressAppViewControllertopic.aspx.
    public partial class InvoiceStatus : ViewController
    {
        SingleChoiceAction changeTaskStatusAction;
        //  ChoiceActionItem notStartedState;
        ChoiceActionItem StartState;
        ChoiceActionItem inProgressState;
        //ChoiceActionItem pausedState;
        ChoiceActionItem completedState;
        // ChoiceActionItem droppedState;
        
        public InvoiceStatus()
        {
            TargetObjectType = typeof(Invoice);

            changeTaskStatusAction = new SingleChoiceAction(this, "ChangeInvoiceStatus", DevExpress.Persistent.Base.PredefinedCategory.Edit);
            changeTaskStatusAction.ItemType = SingleChoiceActionItemType.ItemIsOperation;
            changeTaskStatusAction.ShowItemsOnClick = true;
            changeTaskStatusAction.SelectionDependencyType = SelectionDependencyType.RequireMultipleObjects;
            changeTaskStatusAction.Execute += changeTaskStatusAction_Execute;
            changeTaskStatusAction.ImageName = "Action_Change_State";
            //notStartedState = new ChoiceActionItem("Not Started", TaskStatus.NotStarted);


            StartState = new ChoiceActionItem("Started", Status.Started);
            StartState.ImageName = "State_Task_NotStarted";
            inProgressState = new ChoiceActionItem("In Progress", Status.InProgress);
            inProgressState.ImageName = "State_Task_InProgress";

            completedState = new ChoiceActionItem("Completed", Status.Completed);
            completedState.ImageName = "State_Task_Completed";

            changeTaskStatusAction.Items.AddRange(new ChoiceActionItem[] { StartState, inProgressState, completedState });
            
        }

       

        void changeTaskStatusAction_Execute(object sender, SingleChoiceActionExecuteEventArgs e)
        {
            foreach (Invoice task in View.SelectedObjects)
            {
                task.Status = (Status)e.SelectedChoiceActionItem.Data;
                if (ObjectSpace.IsModified)
                    ObjectSpace.CommitChanges();
            }
        }

        

      

        protected override void OnActivated()
        {
            base.OnActivated();
            View.ObjectSpace.ObjectChanged += ObjectSpace_ObjectChanged;
            View.ObjectSpace.Reloaded += ObjectSpace_Reloaded;
            View.SelectionChanged += View_SelectionChanged;
            UpdateActionItems();
        }

        void ObjectSpace_Reloaded(object sender, EventArgs e)
        {
            UpdateActionItems();
        }

        void View_SelectionChanged(object sender, EventArgs e)
        {
            //IList<Invoice> invoiceSelects = this.View.SelectedObjects.Cast<Invoice>().ToList();
            //foreach (Invoice item in invoiceSelects)
            //{
            //    if (item.Status == Status.Completed)
            //        changeTaskStatusAction.Enabled.SetItemValue("ID1", false);
            //    else
            //        changeTaskStatusAction.Enabled.SetItemValue("ID1", true);
            //}
            UpdateActionItems();
        }

        void ObjectSpace_ObjectChanged(object sender, ObjectChangedEventArgs e)
        {
            if (e.Object == View.CurrentObject)
            {
                UpdateActionItems();
            }
        }

        private void UpdateActionItems()
        {

            StartState.Active[""] = false;
            inProgressState.Active[""] = false;

            completedState.Active[""] = false;

            if (View.SelectedObjects.Count > 0)
            {
                Invoice task = (Invoice)View.SelectedObjects[0];
                if (task != null)
                {
                    if (task.Status == Status.Started)
                    {

                        inProgressState.Active[""] = true;
                    }
                    else if (task.Status == Status.Started)
                    {

                    }
                    else if (task.Status == Status.InProgress)
                    {

                        completedState.Active[""] = true;
                        StartState.Active[""] = true;

                    }

                    else if (task.Status == Status.Completed)
                    {

                    }

                }
            }

        }
    }
}

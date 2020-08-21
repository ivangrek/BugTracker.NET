/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

// This is sample code that gives you an idea of how you could customize the validation 
// and workflow.   What you want to do, ideally, is isolate your customizations to this 
// file so that you can still upgrade to future BugTracker.NET versions without too 
// much trouble.

// If you customize this file, just make sure you don't overlay this file when you
// upgrade.

namespace BugTracker.Web.Core
{
    //Not used now, need TODO
    //using System.Data;
    //using System.Web.UI;
    //using System.Web.UI.HtmlControls;
    //using System.Web.UI.WebControls;

    //public static class Workflow
    //{
    //    public static void CustomAdjustControls(
    //        DataRow bug, // null if a new bug, otherwise the state of the bug now in the db
    //        User user, // the currently logged in user
    //        Page page) // the whole page, so that you can customize other stuff
    //    {
    //        // Uncomment the next line to play with the sample code.
    //        //custom_adjust_controls_sample(bug, user, page);

    //        // This method will be called when a new form is displayed and
    //        // when the bug has been updated.   You'll get the updated status,
    //        // in DataRow bug, but you won't get the updated version of the
    //        // all other fields, so if your workflow depends on them too, you 
    //        // might also want to customize Bug/Create Bug/Update.  

    //        // See the "Note about customizing workflow" in Bug/Create Bug/Update
    //        // for the place to edit.

    //        // If you don't want to change Bug/Create or Bug/Update, but your logic needs
    //        // other fields, you can fetch them from the database yourself in
    //        // your own code here.
    //    }

    //    public static bool CustomValidations(
    //        DataRow bug, // null if a new bug, otherwise the state of the bug now in the db
    //        User user, // the currently logged in user
    //        Page page,
    //        HtmlContainerControl customValidationErrMsg)
    //    {
    //        // This method will be called during validation.
    //        // You'll get some updated fielsd in DataRow bug, but not all.
    //        // You  might also want to customize Bug/Create Bug/Update.  

    //        // See the "Note about customizing workflow" in Bug/Create Bug/Update
    //        // for the place to make the DataRow more accurate.

    //        // Uncomment the next line to play with the sample code.
    //        //return custom_validations_sample(bug, user, page, custom_validation_err_msg);
    //        return true;
    //    }

    //    public static bool CustomValidationsSample(
    //        DataRow bug, // null if a new bug, otherwise the state of the bug now in the db
    //        User user, // the currently logged in user
    //        Page page,
    //        HtmlContainerControl customValidationErrMsg)
    //    {
    //        // Just a stupid example.  Show that we can cross-validate between
    //        // a couple different fields.

    //        var category = (DropDownList)FindControl(page.Controls, "category");
    //        var priority = (DropDownList)FindControl(page.Controls, "priority");

    //        if (category.SelectedItem.Text == "question"
    //            && priority.SelectedItem.Text == "high")
    //        {
    //            customValidationErrMsg.InnerHtml = "Questions cannot be high priority<br>";
    //            return false;
    //        }

    //        customValidationErrMsg.InnerHtml = string.Empty;
    //        return true;
    //    }

    //    // Just to give you an idea of what you could do...
    //    private static void CustomAdjustControlsSample(
    //        DataRow bug, // null if a new bug, otherwise the way the bug is now in the db
    //        User user, // the currently logged in user
    //        Page page) // the options in the dropdown
    //    {
    //        // Adjust the contents of the status dropdown
    //        var status = (DropDownList)FindControl(page.Controls, "status");

    //        if (bug != null) // existing bug
    //        {
    //            // Get the bug's current status.
    //            // See "get_bug_datarow()" in bug.cs for the sql
    //            var statusName = (string)bug["status_name"];

    //            // My logic here first gets rid of all the statuses except the
    //            // selected status.  Then it adds back just the statuses we want to 
    //            // appear in the dropdown.
    //            RemoveAllDropdownItemsThatDontMatchText(status, statusName);

    //            // Add back what we do want.
    //            // Notice that the user is one of the arguments, so
    //            // you can adjust values by user.
    //            // The values here have to match the database values.

    //            if (statusName == "new" || statusName == "re-opened")
    //            {
    //                if (status.Items.FindByValue("2") == null)
    //                    status.Items.Add(new ListItem("in progress", "2"));

    //                if (status.Items.FindByValue("5") == null)
    //                    status.Items.Add(new ListItem("closed", "5"));
    //            }
    //            else if (statusName == "in progress")
    //            {
    //                if (status.Items.FindByValue("1") == null)
    //                    status.Items.Add(new ListItem("new", "1"));

    //                if ((string)bug["category_name"] != "question") // just to show we can reference other fields
    //                    if (status.Items.FindByValue("3") == null)
    //                        status.Items.Add(new ListItem("checked in", "3"));

    //                if (status.Items.FindByValue("5") == null)
    //                    status.Items.Add(new ListItem("closed", "5"));
    //            }
    //            else if (statusName == "checked in")
    //            {
    //                if (status.Items.FindByValue("2") == null)
    //                    status.Items.Add(new ListItem("in progress", "2"));

    //                if (status.Items.FindByValue("5") == null)
    //                    status.Items.Add(new ListItem("closed", "5"));
    //            }
    //            else if (statusName == "closed")
    //            {
    //                if (status.Items.FindByValue("4") == null)
    //                    status.Items.Add(new ListItem("re-opened", "4"));
    //            }
    //        }
    //        else // new bug
    //        {
    //            status.Items.Clear();
    //            status.Items.Add(new ListItem("new", "1"));
    //        }
    //    }

    //    public static void RemoveAllDropdownItemsThatDontMatchText(DropDownList dropdown, string text)
    //    {
    //        for (var i = dropdown.Items.Count - 1; i > -1; i--)
    //            if (dropdown.Items[i].Text != text)
    //                dropdown.Items.Remove(dropdown.Items[i]);
    //    }

    //    // Returns an HTMLControl or WebControl by its id.
    //    public static Control FindControl(ControlCollection controls, string id)
    //    {
    //        foreach (Control c in controls)
    //            if (c.ID == id)
    //            {
    //                return c;
    //            }
    //            else
    //            {
    //                if (c.Controls.Count > 0)
    //                {
    //                    Control c2 = null;
    //                    c2 = FindControl(c.Controls, id);
    //                    if (c2 != null)
    //                        return c2;
    //                }
    //            }

    //        return null;
    //    }
    //}
}
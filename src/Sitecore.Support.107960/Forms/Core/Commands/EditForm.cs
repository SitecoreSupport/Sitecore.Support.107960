using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.Form.Core.Configuration;
using Sitecore.Form.Core.Utility;
using Sitecore.Forms.Core.Data;
using Sitecore.Layouts;
using Sitecore.Shell.Applications.WebEdit.Commands;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;
using Sitecore.WFFM.Abstractions.Dependencies;
using static System.String;
using WebUtil = Sitecore.Web.WebUtil;

namespace Sitecore.Support.Forms.Core.Commands
{
  [Serializable]
  public class EditForm : WebEditCommand
  {
    protected void CheckChanges(ClientPipelineArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      if (args.IsPostBack)
      {
        if (args.Result == "yes")
        {
          args.Parameters["save"] = "1";
          args.IsPostBack = false;
          Context.ClientPage.Start(this, "Run", args);
        }
      }
      else
      {
        var flag = false;
        if (args.Parameters["checksave"] != "0")
        {
          var form = FormItem.GetForm(args.Parameters["formId"]);
          if (GetModifiedFields(form).Any())
          {
            flag = true;
            SheerResponse.Confirm(DependenciesManager.ResourceManager.Localize("ONE_OR_MORE_ITEMS_HAVE_BEEN_CHANGED"));
            args.WaitForPostBack();
          }
        }
        if (!flag)
        {
          args.IsPostBack = false;
          Context.ClientPage.Start(this, "Run", args);
        }
      }
    }

    public override void Execute(CommandContext context)
    {
      Assert.ArgumentNotNull(context, "context");
      var parameters = new NameValueCollection();
      var str = context.Parameters["id"];
      var flag = false;
      if (context.Items.Length > 0)
        flag = FormItem.IsForm(context.Items[0]);
      var formValue = WebUtil.GetFormValue("scLayout");
      parameters["sclayout"] = formValue;
      if (!flag && !IsNullOrEmpty(formValue))
      {
        var documentElement = JsonConvert.DeserializeXmlNode(formValue).DocumentElement;
        if (documentElement != null)
        {
          var xml = documentElement.OuterXml;
          var str4 = WebUtil.GetFormValue("scDeviceID");
          ShortID tid;
          if (ShortID.TryParse(str4, out tid))
            str4 = tid.ToID().ToString();
          var renderingByUniqueId =
            LayoutDefinition.Parse(xml).GetDevice(str4).GetRenderingByUniqueId(context.Parameters["referenceId"]);
          if (renderingByUniqueId != null)
          {
            WebUtil.SetSessionValue(StaticSettings.Mode, StaticSettings.DesignMode);
            if (!IsNullOrEmpty(renderingByUniqueId.Parameters))
              str =
                HttpUtility.UrlDecode(
                  StringUtil.ParseNameValueCollection(renderingByUniqueId.Parameters, (char) 38, (char) 61)["FormID"]);
          }
        }
      }
      var document2 = JsonConvert.DeserializeXmlNode(formValue);
      var key = "PageDesigner";
      if (document2.DocumentElement != null)
      {
        var outerXml = document2.DocumentElement.OuterXml;
        WebUtil.SetSessionValue(key, outerXml);
      }
      if (!IsNullOrEmpty(str))
      {
        parameters["referenceid"] = context.Parameters["referenceId"];
        parameters["formId"] = str;
        parameters["checksave"] = context.Parameters["checksave"] ?? "1";
        if (context.Items.Length > 0)
          parameters["contentlanguage"] = context.Items[0].Language.ToString();
        var args = new ClientPipelineArgs(parameters)
        {
          CustomData =
          {
            {
              "form",
              context.Items[0].Database.GetItem(str)
            }
          }
        };
        Context.ClientPage.Start(this, "CheckChanges", args);
      }
    }

    private IEnumerable<PageEditorField> GetModifiedFields(FormItem form)
    {
      var list = new List<PageEditorField>();
      if (form != null)
        foreach (var field in GetFields(Context.ClientPage.Request.Form))
        {
          var item = StaticSettings.ContextDatabase.GetItem(field.ItemID, field.Language);
          if (form.GetField(field.ItemID) != null || item.ID == form.ID)
          {
            var strB = item[field.FieldID];
            var strA = field.Value;
            if (Compare(strA, strB, StringComparison.OrdinalIgnoreCase) != 0 &&
                CompareOrdinal(strA.TrimWhiteSpaces(), strB.TrimWhiteSpaces()) != 0)
              list.Add(field);
          }
        }
      return list;
    }

    protected void Run(ClientPipelineArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      if (!args.IsPostBack)
      {
        var form = FormItem.GetForm(args.Parameters["formId"]);
        if (form != null && !args.HasResult)
        {
          if (args.Parameters["save"] == "1")
            SaveFields(form);
          var str = args.Parameters["referenceId"];
          var str2 = new UrlString(UIUtil.GetUri("control:Forms.FormDesigner"))
          {
            ["formid"] = form.ID.ToString(),
            ["mode"] = StaticSettings.DesignMode,
            ["db"] = form.Database.Name,
            ["vs"] = form.Version.ToString(),
            ["referenceId"] = form.Version.ToString(),
            ["la"] = args.Parameters["contentlanguage"] ?? form.Language.Name
          };
          if (args.Parameters["referenceId"] != null)
            str2["hdl"] = str;
          var application = ApplicationItem.GetApplication(Path.FormDesignerApplication);
          string width = null;
          string height = null;
          if (application != null)
          {
            width = MainUtil.GetInt(application.Width, 0x4e2).ToString();
            height = MainUtil.GetInt(application.Height, 500).ToString();
          }
          SheerResponse.ShowModalDialog(str2.ToString(), width, height, String.Empty, true);
          SheerResponse.DisableOutput();
          args.WaitForPostBack();
        }
      }
      else if (!IsNullOrEmpty(args.Parameters["scLayout"]))
      {
        ID id;
        SheerResponse.SetAttribute("scLayoutDefinition", "value", args.Parameters["scLayout"]);
        var str5 = args.Parameters["referenceId"];
        if (!IsNullOrEmpty(str5))
          str5 = "r_" + ID.Parse(str5).ToShortID();
        var str6 = args.Parameters["formId"];
        if (ID.TryParse(str6, out id))
          str6 = id.ToShortID().ToString();
        SheerResponse.Eval(
          "window.parent.Sitecore.PageModes.ChromeManager.fieldValuesContainer.children().each(function(e){ if( window.parent.$sc('#form_" +
          str6.ToUpper() + "').find('#' + this.id + '_edit').size() > 0 ) { window.parent.$sc(this).remove() }});");
        SheerResponse.Eval(
          "window.parent.Sitecore.PageModes.ChromeManager.handleMessage('chrome:rendering:propertiescompleted', {controlId : '" +
          str5 + "'});");
      }
    }

    private void SaveFields(FormItem form)
    {
      foreach (var field in GetModifiedFields(form))
      {
        var item = StaticSettings.ContextDatabase.GetItem(field.ItemID);
        item.Editing.BeginEdit();
        item[field.FieldID] = field.Value;
        item.Editing.EndEdit();
      }
    }
  }
}
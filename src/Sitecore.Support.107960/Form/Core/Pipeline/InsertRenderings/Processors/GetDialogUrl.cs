using Sitecore.Diagnostics;
using Sitecore.Form.Core.Configuration;
using Sitecore.Globalization;
using Sitecore.Pipelines.GetRenderingDatasource;
using Sitecore.Text;
using Sitecore.Web;

namespace Sitecore.Support.Form.Core.Pipeline.InsertRenderings.Processors
{
  public class GetDialogUrl
  {
    public void Process(GetRenderingDatasourceArgs args)
    {
      Assert.IsNotNull(args, "args");
      if (args.ContextItemPath != null &&
          (args.RenderingItem.ID == IDs.FormInterpreterID || args.RenderingItem.ID == IDs.FormMvcInterpreterID))
      {
        Assert.IsNotNull(args.ContentDatabase, "args.ContentDatabase");
        Assert.IsNotNull(args.ContextItemPath, "args.ContextItemPath");
        Language contentLanguage = Language.Parse(WebUtil.GetQueryString("la"));
        var item = args.ContentDatabase.GetItem(args.ContextItemPath, string.IsNullOrEmpty(contentLanguage.ToString()) ? args.ContentLanguage : contentLanguage);
        Assert.IsNotNull(item, "currentItem");
        var obj2 = Context.ClientData.GetValue(StaticSettings.PrefixId + StaticSettings.PlaceholderKeyId);
        var str = obj2?.ToString() ?? string.Empty;
        var designMode = StaticSettings.DesignMode;
        if (item.Fields["__renderings"] != null && item.Fields["__renderings"].Value != string.Empty)
        {
          var str3 = new UrlString(UIUtil.GetUri("control:Forms.InsertFormWizard"));
          str3.Add("id", item.ID.ToString());
          str3.Add("db", item.Database.Name);
          str3.Add("la", item.Language.Name);
          str3.Add("vs", item.Version.Number.ToString());
          str3.Add("pe", "1");
          if (!string.IsNullOrEmpty(str))
            str3.Add("placeholder", str);
          if (!string.IsNullOrEmpty(designMode))
            str3.Add("mode", designMode);
          args.DialogUrl = str3.ToString();
          if (string.IsNullOrEmpty(args.CurrentDatasource))
          {
            var str4 = args.RenderingItem["data source"];
            if (!string.IsNullOrEmpty(str4))
              args.CurrentDatasource = str4;
          }
          args.AbortPipeline();
        }
      }
    }
  }
}
namespace Sitecore.Support.Data.Fields
{
  using HtmlAgilityPack;
  using Sitecore.Configuration;
  using Sitecore.Data;
  using Sitecore.Data.Fields;
  using Sitecore.Data.Items;
  using Sitecore.Diagnostics;
  using Sitecore.Layouts;
  using Sitecore.Links;
  using Sitecore.Resources.Media;
  using Sitecore.Text;
  using Sitecore.Web;
  using Sitecore.Web.UI.WebControls.Ribbons;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text.RegularExpressions;
  using System.Web;
  public class HtmlField139799 : CustomField
  {
    // Methods
    public HtmlField139799(Field innerField) : base(innerField)
    {
      Assert.ArgumentNotNull(innerField, "innerField");
    }

    private static void AddLink(LinksValidationResult result, Item targetItem, string targetPath)
    {
      Assert.ArgumentNotNull(result, "result");
      Assert.ArgumentNotNull(targetPath, "targetPath");
      if (targetItem != null)
      {
        result.AddValidLink(targetItem, targetPath);
      }
      else
      {
        result.AddBrokenLink(targetPath);
      }
    }

    private void AddMediaLink(LinksValidationResult result, HtmlNode node)
    {
      Assert.ArgumentNotNull(result, "result");
      Assert.ArgumentNotNull(node, "node");
      string src = node.GetAttributeValue("src", string.Empty);
      if ((!string.IsNullOrEmpty(src) && !this.IsExternalLink(src)) && !MediaManager.Config.MediaPrefixes.All<string>(prefix => (src.IndexOf(prefix, StringComparison.InvariantCulture) == -1)))
      {
        try
        {
          DynamicLink link = DynamicLink.Parse(src);
          Item targetItem = base.InnerField.Database.GetItem(link.ItemId);
          AddLink(result, targetItem, src);
        }
        catch
        {
          AddLink(result, null, src);
        }
      }
    }

    private void AddMediaLinks(LinksValidationResult result, HtmlDocument document)
    {
      Assert.ArgumentNotNull(result, "result");
      Assert.ArgumentNotNull(document, "document");
      HtmlNodeCollection nodes = document.DocumentNode.SelectNodes("//img");
      if (nodes != null)
      {
        foreach (HtmlNode node in (IEnumerable<HtmlNode>)nodes)
        {
          this.AddMediaLink(result, node);
        }
      }
    }

    private void AddTextLink(LinksValidationResult result, HtmlNode node)
    {
      Assert.ArgumentNotNull(result, "result");
      Assert.ArgumentNotNull(node, "node");
      string href = node.GetAttributeValue("href", string.Empty);
      if (!string.IsNullOrEmpty(href) && !this.IsExternalLink(href))
      {
        List<string> source = new List<string> { "~/link.aspx?" };
        source.AddRange(MediaManager.Config.MediaPrefixes);
        if (!source.All<string>(prefix => (href.IndexOf(prefix, StringComparison.InvariantCulture) == -1)))
        {
          try
          {
            Item linkedToItem = this.GetLinkedToItem(href);
            AddLink(result, linkedToItem, href);
          }
          catch
          {
            AddLink(result, null, href);
          }
        }
      }
    }

    private void AddTextLinks(LinksValidationResult result, HtmlDocument document)
    {
      Assert.ArgumentNotNull(result, "result");
      Assert.ArgumentNotNull(document, "document");
      HtmlNodeCollection nodes = document.DocumentNode.SelectNodes("//a[@href]");
      if (nodes != null)
      {
        foreach (HtmlNode node in (IEnumerable<HtmlNode>)nodes)
        {
          this.AddTextLink(result, node);
        }
      }
    }

    [Obsolete("Use XHtml.Convert() instead.")]
    public static string ConvertToXHtml(string html)
    {
      Assert.ArgumentNotNull(html, "html");
      return XHtml.Convert(html);
    }

    private ID GetLinkedItemID(string href)
    {
      DynamicLink link;
      Assert.ArgumentNotNull(href, "href");
      try
      {
        link = DynamicLink.Parse(href);
      }
      catch (InvalidLinkFormatException)
      {
        return null;
      }
      return link.ItemId;
    }

    private Item GetLinkedToItem(string href)
    {
      DynamicLink link;
      Assert.ArgumentNotNull(href, "href");
      try
      {
        link = DynamicLink.Parse(href);
      }
      catch (InvalidLinkFormatException)
      {
        return null;
      }
      return base.InnerField.Database.GetItem(link.ItemId);
    }

    public virtual string GetPlainText()
    {
      string input = base.InnerField.GetValue(true);
      if (input == null)
      {
        return null;
      }
      return HttpUtility.HtmlDecode(Regex.Replace(input, "<[^>]*>", string.Empty));
    }

    public override List<WebEditButton> GetWebEditButtons()
    {
      List<WebEditButton> list = new List<WebEditButton>();
      string path = StringUtil.GetString(new string[] { base.InnerField.Source, Settings.HtmlEditor.DefaultProfile });
      Item item = Client.GetDatabaseNotNull("core").GetItem(path);
      if (item != null)
      {
        Item item2 = item.Children["WebEdit Buttons"];
        if (item2 == null)
        {
          return list;
        }
        foreach (Item item3 in item2.Children)
        {
          WebEditButton button = new WebEditButton();
          if (item3.TemplateID == Ribbon.Separator)
          {
            button.IsDivider = true;
          }
          else
          {
            button.Header = item3["Header"];
            button.Icon = item3["Icon"];
            button.Click = item3["Click"];
            button.Tooltip = item3["Tooltip"];
          }
          if (UIUtil.SupportsInlineEditing() || button.Click.Contains("edithtml"))
          {
            list.Add(button);
          }
        }
      }
      return list;
    }

    private bool IsExternalLink(string link)
    {
      Assert.ArgumentNotNull(link, "link");
      Uri url = null;
      if (HttpContext.Current != null)
      {
        url = HttpContext.Current.Request.Url;
      }
      if ((url == null) && !string.IsNullOrEmpty(Globals.ServerUrl))
      {
        url = new Uri(Globals.ServerUrl);
      }
      return (((url != null) && link.StartsWith(url.Scheme, StringComparison.InvariantCultureIgnoreCase)) && !link.StartsWith($"{url.Scheme}://{url.Host}/", StringComparison.InvariantCultureIgnoreCase));
    }

    private static string MediaEvaluator(Match match)
    {
      Assert.ArgumentNotNull(match, "match");
      string input = match.Value;
      if (input.IndexOf("src=\"", StringComparison.OrdinalIgnoreCase) >= 0)
      {
        input = Regex.Replace(input, "src=\"([^\"]*)\"", new MatchEvaluator(HtmlField139799.MediaEvaluator2), RegexOptions.Singleline | RegexOptions.IgnoreCase);
      }
      else if (input.IndexOf("src='", StringComparison.OrdinalIgnoreCase) >= 0)
      {
        input = Regex.Replace(input, "src='([^']*)'", new MatchEvaluator(HtmlField139799.MediaEvaluator2), RegexOptions.Singleline | RegexOptions.IgnoreCase);
      }
      else
      {
        input = Regex.Replace(input, @"src=([^\s]*)", new MatchEvaluator(HtmlField139799.MediaEvaluator2), RegexOptions.Singleline | RegexOptions.IgnoreCase);
      }
      return Assert.ResultNotNull<string>(input);
    }

    private static string MediaEvaluator2(Match match)
    {
      Assert.ArgumentNotNull(match, "match");
      string text = match.Value;
      int start = -1;
      foreach (string str2 in MediaManager.Config.MediaPrefixes)
      {
        start = text.IndexOf(str2, StringComparison.InvariantCulture);
        if (start >= 0)
        {
          break;
        }
      }
      if (start >= 0)
      {
        int index = text.IndexOf('"');
        if (index < 0)
        {
          index = text.IndexOf('\'');
        }
        if (index < 0)
        {
          text = StringUtil.Mid(text, start);
        }
        else
        {
          Uri url = HttpContext.Current.Request.Url;
          if (!text.Substring(index + 1).StartsWith($"{url.Scheme}://{url.Host}"))
          {
            return Assert.ResultNotNull<string>(text);
          }
          text = StringUtil.Mid(text, start, (text.Length - start) - 1);
        }
        text = "src=\"" + text + "\"";
      }
      return Assert.ResultNotNull<string>(text);
    }

    public static implicit operator HtmlField139799(Field field)
    {
      if (field != null)
      {
        return new HtmlField139799(field);
      }
      return null;
    }

    public override void Relink(ItemLink itemLink, Item newLink)
    {
      Assert.ArgumentNotNull(itemLink, "itemLink");
      Assert.ArgumentNotNull(newLink, "newLink");
      if (base.InnerField.HasValue || (base.InnerField.GetInheritedValue() != null))
      {
        HtmlDocument document = new HtmlDocument
        {
          OptionWriteEmptyNodes = true
        };
        document.LoadHtml(base.Value);
        if (this.RelinkTextLink(document, itemLink, newLink) | this.RelinkMediaLinks(document, itemLink, newLink))
        {
          RuntimeHtml.FixBullets(document);
          RuntimeHtml.FixSelectOptions(document);
          base.Value = this.TagsFix(document);
        }
      }
    }

    private bool RelinkMediaLinks(HtmlDocument document, ItemLink itemLink, Item newLink)
    {
      Assert.ArgumentNotNull(document, "document");
      Assert.ArgumentNotNull(itemLink, "itemLink");
      Assert.ArgumentNotNull(newLink, "newLink");
      HtmlNodeCollection nodes = document.DocumentNode.SelectNodes("//img");
      if (nodes == null)
      {
        return false;
      }
      bool flag = false;
      foreach (HtmlNode node in (IEnumerable<HtmlNode>)nodes)
      {
        string attributeValue = node.GetAttributeValue("src", string.Empty);
        if (!string.IsNullOrEmpty(attributeValue))
        {
          ID linkedItemID = this.GetLinkedItemID(attributeValue);
          if ((!linkedItemID.IsNull) && (linkedItemID == itemLink.TargetItemID))
          {
            MediaUrlOptions shellOptions = MediaUrlOptions.GetShellOptions();
            string mediaUrl = MediaManager.GetMediaUrl(newLink, shellOptions);
            node.SetAttributeValue("src", ReplaceUrlPath(attributeValue, mediaUrl));
            if (node.GetAttributeValue("sc_mediaid", (string)null) != null)
            {
              node.SetAttributeValue("sc_url", newLink.ID.ToString());
            }
            flag = true;
          }
        }
      }
      return flag;
    }

    private bool RelinkTextLink(HtmlDocument document, ItemLink itemLink, Item newLink)
    {
      Assert.ArgumentNotNull(document, "document");
      Assert.ArgumentNotNull(itemLink, "itemLink");
      Assert.ArgumentNotNull(newLink, "newLink");
      HtmlNodeCollection nodes = document.DocumentNode.SelectNodes("//a[@href]");
      if (nodes == null)
      {
        return false;
      }
      bool flag = false;
      foreach (HtmlNode node in (IEnumerable<HtmlNode>)nodes)
      {
        string attributeValue = node.GetAttributeValue("href", string.Empty);
        if (!string.IsNullOrEmpty(attributeValue))
        {
          ID linkedItemID = this.GetLinkedItemID(attributeValue);
          if ((!linkedItemID.IsNull) && (linkedItemID == itemLink.TargetItemID))
          {
            string newHref = newLink.Paths.IsMediaItem ? MediaManager.GetMediaUrl(new MediaItem(newLink), MediaUrlOptions.GetShellOptions()) : LinkManager.GetDynamicUrl(newLink, LinkUrlOptions.Empty);
            node.SetAttributeValue("href", ReplaceUrlPath(attributeValue, newHref));
            if (node.GetAttributeValue("sc_url", (string)null) != null)
            {
              node.SetAttributeValue("sc_url", newLink.ID.ToString());
            }
            flag = true;
          }
        }
      }
      return flag;
    }

    public override void RemoveLink(ItemLink itemLink)
    {
      Assert.ArgumentNotNull(itemLink, "itemLink");
      if (base.InnerField.HasValue || (base.InnerField.GetInheritedValue() != null))
      {
        HtmlDocument document = new HtmlDocument
        {
          OptionWriteEmptyNodes = true
        };
        document.LoadHtml(base.Value);
        if (this.RemoveTextLinks(itemLink, document) | this.RemoveMediaLinks(itemLink, document))
        {
          RuntimeHtml.FixBullets(document);
          RuntimeHtml.FixSelectOptions(document);
          base.Value = this.TagsFix(document);
        }
      }
    }

    private bool RemoveMediaLinks(ItemLink itemLink, HtmlDocument document)
    {
      Assert.ArgumentNotNull(itemLink, "itemLink");
      Assert.ArgumentNotNull(document, "document");
      HtmlNodeCollection nodes = document.DocumentNode.SelectNodes("//img");
      if (nodes == null)
      {
        return false;
      }
      bool flag = false;
      foreach (HtmlNode node in (IEnumerable<HtmlNode>)nodes)
      {
        string attributeValue = node.GetAttributeValue("src", string.Empty);
        if (!string.IsNullOrEmpty(attributeValue))
        {
          ID linkedItemID = this.GetLinkedItemID(attributeValue);
          if ((!linkedItemID.IsNull) && (linkedItemID == itemLink.TargetItemID))
          {
            node.ParentNode.RemoveChild(node, true);
            flag = true;
          }
        }
      }
      return flag;
    }

    private bool RemoveTextLinks(ItemLink itemLink, HtmlDocument document)
    {
      Assert.ArgumentNotNull(itemLink, "itemLink");
      Assert.ArgumentNotNull(document, "document");
      HtmlNodeCollection nodes = document.DocumentNode.SelectNodes("//a[@href]");
      if (nodes == null)
      {
        return false;
      }
      bool flag = false;
      foreach (HtmlNode node in (IEnumerable<HtmlNode>)nodes)
      {
        string attributeValue = node.GetAttributeValue("href", string.Empty);
        if (!string.IsNullOrEmpty(attributeValue))
        {
          ID linkedItemID = this.GetLinkedItemID(attributeValue);
          if ((!linkedItemID.IsNull) && (linkedItemID == itemLink.TargetItemID))
          {
            node.ParentNode.RemoveChild(node, true);
            flag = true;
          }
        }
      }
      return flag;
    }

    private static string ReplaceUrlPath(string oldHref, string newHref)
    {
      Assert.ArgumentNotNull(oldHref, "oldHref");
      Assert.ArgumentNotNull(newHref, "newHref");
      UrlString str = new UrlString(newHref);
      UrlString str2 = new UrlString(oldHref);
      foreach (string str3 in str2.Parameters)
      {
        if (((str3 != "_id") && (str3 != "_site")) && ((str3 != "_lang") && (str3 != "_z")))
        {
          str[str3] = str2[str3];
        }
      }
      return str.ToString();
    }

    public static string TightenRelativeImageLinks(string html)
    {
      Assert.ArgumentNotNull(html, "html");
      html = Regex.Replace(html, "<img([^>]*)>", new MatchEvaluator(HtmlField139799.MediaEvaluator), RegexOptions.Singleline | RegexOptions.IgnoreCase);
      html = Regex.Replace(html, "<a([^>]*)>", new MatchEvaluator(HtmlField139799.MediaEvaluator), RegexOptions.Singleline | RegexOptions.IgnoreCase);
      return Assert.ResultNotNull<string>(html);
    }

    public override void ValidateLinks(LinksValidationResult result)
    {
      Assert.ArgumentNotNull(result, "result");
      string str = base.Value;
      if (!string.IsNullOrEmpty(str))
      {
        HtmlDocument document = new HtmlDocument
        {
          OptionWriteEmptyNodes = true
        };
        document.LoadHtml(str);
        this.AddTextLinks(result, document);
        this.AddMediaLinks(result, document);
      }
    }

    /// <summary>
    /// fix for #139799
    /// </summary>
    /// <param name="doc"></param>
    /// <returns></returns>
    private string TagsFix(HtmlDocument doc)
    {
      HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//img");
      foreach (HtmlNode node in (IEnumerable<HtmlNode>)nodes)
      {
        string str = node.Attributes["src"].Value;
        if (str.IndexOf("&", StringComparison.CurrentCultureIgnoreCase) != str.IndexOf("&amp;", StringComparison.CurrentCultureIgnoreCase))
        {
          node.SetAttributeValue("src", str.Replace("&", "&amp;"));
        }
      }
      return doc.DocumentNode.OuterHtml;
    }
  }
}
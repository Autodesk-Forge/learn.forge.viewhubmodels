using Autodesk.Forge;
using Autodesk.Forge.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;

namespace forgesample.Controllers
{
  public class DataManagementController : ApiController
  {
    private Credentials Credentials { get; set; }

    [HttpGet]
    [Route("api/forge/datamanagement")]
    public async Task<IList<jsTreeNode>> GetTreeNodeAsync([FromUri]string id)
    {
      Credentials = await Credentials.FromSessionAsync();
      if (Credentials == null)
      {
        return null;
      }

      IList<jsTreeNode> nodes = new List<jsTreeNode>();

      if (id == "#") // root
        return await GetHubsAsync();
      else
      {
        string[] idParams = id.Split('/');
        string resource = idParams[idParams.Length - 2];
        switch (resource)
        {
          case "hubs": // hubs node selected/expanded, show projects
            return await GetProjectsAsync(id);
          case "projects": // projects node selected/expanded, show root folder contents
            return await GetProjectContents(id);
          case "folders": // folders node selected/expanded, show folder contents
            return await GetFolderContents(id);
          case "items":
            return await GetItemVersions(id);
        }
      }

      return nodes;
    }



    private async Task<IList<jsTreeNode>> GetHubsAsync()
    {
      IList<jsTreeNode> nodes = new List<jsTreeNode>();

      HubsApi hubsApi = new HubsApi();
      hubsApi.Configuration.AccessToken = Credentials.TokenInternal;

      var hubs = await hubsApi.GetHubsAsync();
      string urn = string.Empty;
      foreach (KeyValuePair<string, dynamic> hubInfo in new DynamicDictionaryItems(hubs.data))
      {
        string nodeType = "hubs";
        switch ((string)hubInfo.Value.attributes.extension.type)
        {
          case "hubs:autodesk.core:Hub":
            nodeType = "hubs";
            break;
          case "hubs:autodesk.a360:PersonalHub":
            nodeType = "personalHub";
            break;
          case "hubs:autodesk.bim360:Account":
            nodeType = "bim360Hubs";
            break;
        }
        jsTreeNode hubNode = new jsTreeNode(hubInfo.Value.links.self.href, hubInfo.Value.attributes.name, nodeType, true);
        nodes.Add(hubNode);
      }

      return nodes;
    }

    private async Task<IList<jsTreeNode>> GetProjectsAsync(string href)
    {
      IList<jsTreeNode> nodes = new List<jsTreeNode>();
      string[] idParams = href.Split('/');

      string hubId = idParams[idParams.Length - 1];
      ProjectsApi projectsApi = new ProjectsApi();
      projectsApi.Configuration.AccessToken = Credentials.TokenInternal;
      var projects = await projectsApi.GetHubProjectsAsync(hubId);
      foreach (KeyValuePair<string, dynamic> projectInfo in new DynamicDictionaryItems(projects.data))
      {
        string nodeType = "projects";
        switch ((string)projectInfo.Value.attributes.extension.type)
        {
          case "projects:autodesk.core:Projec":
            nodeType = "a360projects";
            break;
          case "projects:autodesk.bim360:Project":
            nodeType = "bim360projects";
            break;
        }
        jsTreeNode projectNode = new jsTreeNode(projectInfo.Value.links.self.href, projectInfo.Value.attributes.name, nodeType, true);
        nodes.Add(projectNode);
      }

      return nodes;
    }

    private async Task<IList<jsTreeNode>> GetProjectContents(string href)
    {
      IList<jsTreeNode> nodes = new List<jsTreeNode>();

      string[] idParams = href.Split('/');
      string hubId = idParams[idParams.Length - 3];
      string projectId = idParams[idParams.Length - 1];

      ProjectsApi projectApi = new ProjectsApi();
      projectApi.Configuration.AccessToken = Credentials.TokenInternal;
      var project = await projectApi.GetProjectAsync(hubId, projectId);
      var rootFolderHref = project.data.relationships.rootFolder.meta.link.href;

      return await GetFolderContents(rootFolderHref);
    }

    private async Task<IList<jsTreeNode>> GetFolderContents(string href)
    {
      IList<jsTreeNode> nodes = new List<jsTreeNode>();

      string[] idParams = href.Split('/');
      string folderId = idParams[idParams.Length - 1];
      string projectId = idParams[idParams.Length - 3];

      FoldersApi folderApi = new FoldersApi();
      folderApi.Configuration.AccessToken = Credentials.TokenInternal;
      var folderContents = await folderApi.GetFolderContentsAsync(projectId, folderId);
      foreach (KeyValuePair<string, dynamic> folderContentItem in new DynamicDictionaryItems(folderContents.data))
      {
        string displayName = folderContentItem.Value.attributes.displayName;
        if (string.IsNullOrWhiteSpace(displayName))
        {
          // BIM 360 related documents don't have displayName
          // need to ask each one for a name
          ItemsApi itemsApi = new ItemsApi();
          itemsApi.Configuration.AccessToken = Credentials.TokenInternal;
          dynamic item = await itemsApi.GetItemAsync(projectId, folderContentItem.Value.id);
          displayName = item.included[0].attributes.displayName;
        }

        jsTreeNode itemNode = new jsTreeNode(folderContentItem.Value.links.self.href, displayName, (string)folderContentItem.Value.type, true);

        nodes.Add(itemNode);
      }

      return nodes;
    }

    private async Task<IList<jsTreeNode>> GetItemVersions(string href)
    {
      IList<jsTreeNode> nodes = new List<jsTreeNode>();

      string[] idParams = href.Split('/');
      string itemId = idParams[idParams.Length - 1];
      string projectId = idParams[idParams.Length - 3];

      ItemsApi itemApi = new ItemsApi();
      itemApi.Configuration.AccessToken = Credentials.TokenInternal;
      var versions = await itemApi.GetItemVersionsAsync(projectId, itemId);
      foreach (KeyValuePair<string, dynamic> version in new DynamicDictionaryItems(versions.data))
      {
        DateTime versionDate = version.Value.attributes.lastModifiedTime;
        string urn = string.Empty;
        try { urn = (string)version.Value.relationships.derivatives.data.id; }
        catch { urn = "not_available"; } // some BIM 360 versions don't have viewable
        jsTreeNode node = new jsTreeNode(urn, versionDate.ToString("dd/MM/yy HH:mm:ss"), "versions", false);
        nodes.Add(node);
      }

      return nodes;
    }
  }

  public class jsTreeNode
  {
    public jsTreeNode(string id, string text, string type, bool children)
    {
      this.id = id;
      this.text = text;
      this.type = type;
      this.children = children;
    }

    public string id { get; set; }
    public string text { get; set; }
    public string type { get; set; }
    public bool children { get; set; }
  }
}

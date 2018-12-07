/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by Forge Partner Development
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
/////////////////////////////////////////////////////////////////////

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
    /// <summary>
    /// Credentials on this request
    /// </summary>
    private Credentials Credentials { get; set; }

    /// <summary>
    /// GET TreeNode passing the ID
    /// </summary>
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

      // the API SDK
      HubsApi hubsApi = new HubsApi();
      hubsApi.Configuration.AccessToken = Credentials.TokenInternal;

      var hubs = await hubsApi.GetHubsAsync();
      foreach (KeyValuePair<string, dynamic> hubInfo in new DynamicDictionaryItems(hubs.data))
      {
        // check the type of the hub to show an icon
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

        // create a treenode with the values
        jsTreeNode hubNode = new jsTreeNode(hubInfo.Value.links.self.href, hubInfo.Value.attributes.name, nodeType, true);
        nodes.Add(hubNode);
      }

      return nodes;
    }

    private async Task<IList<jsTreeNode>> GetProjectsAsync(string href)
    {
      IList<jsTreeNode> nodes = new List<jsTreeNode>();

      // the API SDK
      ProjectsApi projectsApi = new ProjectsApi();
      projectsApi.Configuration.AccessToken = Credentials.TokenInternal;

      // extract the hubId from the href
      string[] idParams = href.Split('/');
      string hubId = idParams[idParams.Length - 1];

      var projects = await projectsApi.GetHubProjectsAsync(hubId);
      foreach (KeyValuePair<string, dynamic> projectInfo in new DynamicDictionaryItems(projects.data))
      {
        // check the type of the project to show an icon
        string nodeType = "projects";
        switch ((string)projectInfo.Value.attributes.extension.type)
        {
          case "projects:autodesk.core:Project":
            nodeType = "a360projects";
            break;
          case "projects:autodesk.bim360:Project":
            nodeType = "bim360projects";
            break;
        }

        // create a treenode with the values
        jsTreeNode projectNode = new jsTreeNode(projectInfo.Value.links.self.href, projectInfo.Value.attributes.name, nodeType, true);
        nodes.Add(projectNode);
      }

      return nodes;
    }

    private async Task<IList<jsTreeNode>> GetProjectContents(string href)
    {
      IList<jsTreeNode> nodes = new List<jsTreeNode>();

      // the API SDK
      ProjectsApi projectApi = new ProjectsApi();
      projectApi.Configuration.AccessToken = Credentials.TokenInternal;

      // extract the hubId & projectId from the href
      string[] idParams = href.Split('/');
      string hubId = idParams[idParams.Length - 3];
      string projectId = idParams[idParams.Length - 1];

      var folders = await projectApi.GetProjectTopFoldersAsync(hubId, projectId);
      foreach (KeyValuePair<string, dynamic> folder in new DynamicDictionaryItems(folders.data))
      {
        nodes.Add(new jsTreeNode(folder.Value.links.self.href, folder.Value.attributes.displayName, "folders", true));
      }
      return nodes;
    }

    private async Task<IList<jsTreeNode>> GetFolderContents(string href)
    {
      IList<jsTreeNode> nodes = new List<jsTreeNode>();

      // the API SDK
      FoldersApi folderApi = new FoldersApi();
      folderApi.Configuration.AccessToken = Credentials.TokenInternal;

      // extract the projectId & folderId from the href
      string[] idParams = href.Split('/');
      string folderId = idParams[idParams.Length - 1];
      string projectId = idParams[idParams.Length - 3];

      var folderContents = await folderApi.GetFolderContentsAsync(projectId, folderId);
      foreach (KeyValuePair<string, dynamic> folderContentItem in new DynamicDictionaryItems(folderContents.data))
      {
        string displayName = folderContentItem.Value.attributes.displayName;
        jsTreeNode itemNode = new jsTreeNode(folderContentItem.Value.links.self.href, displayName, (string)folderContentItem.Value.type, true);
        nodes.Add(itemNode);
      }

      return nodes;
    }

    private async Task<IList<jsTreeNode>> GetItemVersions(string href)
    {
      IList<jsTreeNode> nodes = new List<jsTreeNode>();

      // the API SDK
      ItemsApi itemApi = new ItemsApi();
      itemApi.Configuration.AccessToken = Credentials.TokenInternal;

      // extract the projectId & itemId from the href
      string[] idParams = href.Split('/');
      string itemId = idParams[idParams.Length - 1];
      string projectId = idParams[idParams.Length - 3];

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
}

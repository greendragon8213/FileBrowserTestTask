﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using AutoMapper;
using FileBrowsing.Common;
using FileBrowsing.Extensions;
using FileBrowsing.Models;

namespace FileBrowsing.Controllers
{
    public class BrowserController : ApiController
    {
        private const string Root = "My Computer";
        private readonly IMapper _mapper;

        public BrowserController(IMapper mapper)
        {
            _mapper = mapper;
        }

        public HttpResponseMessage GetAllNodesFromMyComputer()
        {
            var folder = new FolderNode(null, null, Root);

            var allDrives = DriveInfo.GetDrives().ToList();
            folder.SubFolders = _mapper.Map<List<DriveInfo>, List<FolderNode>>(allDrives);

            return Request.CreateResponse(HttpStatusCode.OK, folder);
        }

        public HttpResponseMessage GetAllNodesByFolderPath(string path)
        {
            var directoryInfo = new DirectoryInfo(path);
            var allNodesInFolderNode = _mapper.Map<DirectoryInfo, FolderNode>(directoryInfo);

            var dirs = directoryInfo.GetAvailableSubDirectories().ToList();
            allNodesInFolderNode.SubFolders = _mapper.Map<List<DirectoryInfo>, List<FolderNode>>(dirs);

            var files = directoryInfo.GetAvailableNestedFiles().ToList();
            allNodesInFolderNode.NestedFiles = _mapper.Map<List<FileInfo>, List<FileNode>>(files);

            return Request.CreateResponse(HttpStatusCode.OK, allNodesInFolderNode);
        }

        [HttpPost]
        public async Task<HttpResponseMessage> GetFilesCountFromAllDisks([FromBody] Filter filter)
        {
            var allDrives = DriveInfo.GetDrives();

            Expression<Func<FileInfo, bool>> getFilesCountByFileSizePredicate =
                fi =>
                    fi.Length < filter.MaxFileLengthMb * Constants.BytesCountInMegabyte &&
                    fi.Length > filter.MinFileLengthMb * Constants.BytesCountInMegabyte;

            int filesCount = await Task.Factory.StartNew(() =>
                allDrives.Sum(drive => new DirectoryInfo(drive.Name)
                .GetAllAvailableFilesByPredicate(getFilesCountByFileSizePredicate)
                .Result
                .Count()));

            return Request.CreateResponse(HttpStatusCode.OK, filesCount);
        }

        [HttpPost]
        public async Task<HttpResponseMessage> GetFilesCount(string path, [FromBody] Filter filter)
        {
            if (path == "")//ToDo
                return Request.CreateResponse(HttpStatusCode.NotFound);

            Expression<Func<FileInfo, bool>> getFilesCountByFileSizePredicate =
                fi =>
                    fi.Length < filter.MaxFileLengthMb * Constants.BytesCountInMegabyte &&
                    fi.Length > filter.MinFileLengthMb * Constants.BytesCountInMegabyte;

            int filesCount = await Task.Factory.StartNew(() => new DirectoryInfo(path)
                    .GetAllAvailableFilesByPredicate(getFilesCountByFileSizePredicate).Result.Count());

            return Request.CreateResponse(HttpStatusCode.OK, filesCount);
        }
    }
}

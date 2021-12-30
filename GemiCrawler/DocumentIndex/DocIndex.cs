﻿
using System.Collections.Generic;
using System.Data;
using System.Linq;
using EFCore.BulkExtensions;

using Gemi.Net;
using GemiCrawler.DocumentIndex.Db;
using GemiCrawler.GemText;

namespace GemiCrawler.DocumentIndex
{
    public class DocIndex
    {
        string StoragePath;

        public DocIndex(string storagePath)
        {
            StoragePath = storagePath;
            //create and distory a DBContext to force the DB to be there
            var db = new DocIndexDbContext(storagePath);
            db.Database.EnsureCreated();
        }

        public void Close()
        {
            ///nop since we are not caching any writes
        }

        private long toLong(ulong ulongValue)
            => unchecked((long)ulongValue);

        private ulong toULong(long longValue)
            => unchecked((ulong)longValue);

        public void StoreMetaData(GemiUrl url, GemiResponse resp, int outboundLinkCount)
        {
            var entry = new StoredDocEntry
            {
                DBDocID = toLong(url.DocID),
                FirstSeen = System.DateTime.Now,
                LastVisit = System.DateTime.Now,

                Url = resp.RequestUrl.NormalizedUrl,
                Domain = resp.RequestUrl.Hostname,
                Port = resp.RequestUrl.Port,

                ConnectStatus = resp.ConnectStatus,
                Status = resp.StatusCode,
                Meta = resp.Meta,

                MimeType = resp.MimeType,
                BodySkipped = resp.BodySkipped,
                BodySize = resp.BodySize,
                BodyHash = resp.BodyHash,
                Title = TitleFinder.ExtractTitle(resp),

                ConnectTime = resp.ConnectTime,
                DownloadTime = resp.DownloadTime,
                OutboundLinks = outboundLinkCount
            };

            using (var db = new DocIndexDbContext(StoragePath))
            {
                db.DocEntries.Add(entry);
                db.SaveChanges();
            }
        }

        public void StoreLinks(GemiUrl sourcePage, List<FoundLink> links)
        {
            using (var db = new DocIndexDbContext(StoragePath))
            {
                db.BulkInsert(links.Distinct().Select(link => new StoredLinkEntry
                {
                    DBSourceDocID = toLong(sourcePage.DocID),
                    DBTargetDocID = toLong(link.Url.DocID),
                    IsExternal = link.IsExternal,
                    LinkText = link.LinkText
                }).ToList());

                db.SaveChanges();
                //7102968342834367033
            }
        }


    }
}

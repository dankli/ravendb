// -----------------------------------------------------------------------
//  <copyright file="QueryIn.cs" company="Hibernating Rhinos LTD">
//      Copyright (c) Hibernating Rhinos LTD. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using FastTests;
using Raven.NewClient.Abstractions.Indexing;
using Raven.NewClient.Client.Indexing;
using Raven.NewClient.Client.Linq;
using Raven.NewClient.Operations.Databases.Indexes;
using Xunit;

namespace SlowTests.MailingList
{
    public class QueryIn : RavenNewTestBase
    {
        [Fact]
        public void ShouldWork()
        {
            var idents = new[] { 1, 2, 3, 4, 5, 6, 7 };
            var index = 0;

            using (var store = GetDocumentStore())
            {
                for (var i = 0; i < 64; i++)
                {
                    using (var session = store.OpenSession())
                    {
                        for (var j = 0; j < 10; j++)
                            session.Store(new MyEntity
                            {
                                ImageId = idents[index++ % idents.Length],
                            });
                        session.SaveChanges();
                    }
                }

                store.Admin.Send(new PutIndexOperation("TestIndex", new IndexDefinition
                {
                    Maps = {
                        @"docs.MyEntities.Select(entity => new {
                                    Text = entity.Text,
                                    ImageId = entity.ImageId
                                })"
                    },
                    Fields = new Dictionary<string, IndexFieldOptions>
                    {
                        { "Text", new IndexFieldOptions { Indexing = FieldIndexing.Analyzed } }
                    }
                }));

                using (var session = store.OpenSession())
                {
                    Assert.NotEmpty(session
                                        .Query<MyEntity>("TestIndex")
                                        .Customize(x => x.WaitForNonStaleResults(TimeSpan.FromMinutes(5)))
                                        .Where(x => x.ImageId.In(new[] { 67, 66, 78, 99, 700, 6 }))
                                        .Take(1024));
                    Assert.NotEmpty(session
                                            .Query<MyEntity>("TestIndex")
                                            .Customize(x => x.WaitForNonStaleResults(TimeSpan.FromMinutes(5)))
                                            .Where(x => x.ImageId.In(new[] { 67, 23, 66, 78, 99, 700, 6 }))
                                            .Take(1024));
                }
            }
        }

        private class MyEntity
        {
            public string Id { get; set; }
            public int ImageId { get; set; }
            public string Text { get; set; }
        }

    }
}

// -----------------------------------------------------------------------
//  <copyright file="CanQueryOnTrue.cs" company="Hibernating Rhinos LTD">
//      Copyright (c) Hibernating Rhinos LTD. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading.Tasks;
using FastTests;
using Raven.NewClient.Client.Linq;
using Xunit;

namespace SlowTests.MailingList
{
    public class CanQueryOnTrue : RavenNewTestBase
    {
        private class Item
        {
            public string Name { get; set; }
        }

        [Fact]
        public void CanQuery()
        {
            using (var store = GetDocumentStore())
            {
                using (var s = store.OpenSession())
                {
                    var e = Assert.Throws<ArgumentException>(
                        () => s.Query<Item>().Where(_ => true).Where(x => x.Name == "oren").ToList());

                    Assert.Equal("Constants expressions such as Where(x => true) are not allowed in the RavenDB queries", e.InnerException.Message);
                }
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using FastTests;
using Raven.NewClient.Client;
using Raven.NewClient.Client.Indexing;
using Raven.NewClient.Client.Linq;
using Raven.NewClient.Operations.Databases.Indexes;
using Xunit;

namespace SlowTests.MailingList
{
    public class DynamicQueryIndexSelection : RavenNewTestBase
    {
        [Fact]
        public void DynamicQueryWillChooseStaticIndex()
        {

            using (var store = GetDocumentStore())
            {

                // With the proper fix in RavenQueryProviderProcessor<T>.GetMember(Expression expression), this should produce member paths like
                // Bar_SomeDictionary_Key Same as the dynamic query gets on the server side.
                // See commented query below.

                // store.Conventions.FindPropertyNameForIndex = (indexedType, indexedName, path, prop) => (path + prop).Replace(".", "_").Replace(",", "_");

                using (var session = store.OpenSession())
                {

                    var foo = new Foo()
                    {

                        SomeProperty = "Some Data",
                        Bar =
                            new Bar() { SomeDictionary = new Dictionary<string, string>() { { "KeyOne", "ValueOne" }, { "KeyTwo", "ValueTwo" } } }

                    };

                    session.Store(foo);

                    foo = new Foo()
                    {

                        SomeProperty = "Some More Data",

                    };

                    session.Store(foo);

                    foo = new Foo()
                    {

                        SomeProperty = "Some Even More Data",
                        Bar = new Bar() { SomeDictionary = new Dictionary<string, string>() { { "KeyThree", "ValueThree" } } }

                    };

                    session.Store(foo);

                    foo = new Foo()
                    {

                        SomeProperty = "Some Even More Data",
                        Bar = new Bar() { SomeOtherDictionary = new Dictionary<string, string>() { { "KeyFour", "ValueFour" } } }

                    };

                    session.Store(foo);

                    session.SaveChanges();

                    store.Admin.Send(new PutIndexOperation("Foos/TestDynamicQueries", new IndexDefinition()
                    {
                        Maps =
                        {
                            @"from doc in docs.Foos
                                from docBarSomeOtherDictionaryItem in ((IEnumerable<dynamic>)doc.Bar.SomeOtherDictionary).DefaultIfEmpty()
                                from docBarSomeDictionaryItem in ((IEnumerable<dynamic>)doc.Bar.SomeDictionary).DefaultIfEmpty()
                                select new
                                {
                                    Bar_SomeOtherDictionary_Value = docBarSomeOtherDictionaryItem.Value,
                                    Bar_SomeOtherDictionary_Key = docBarSomeOtherDictionaryItem.Key,
                                    Bar_SomeDictionary_Value = docBarSomeDictionaryItem.Value,
                                    Bar_SomeDictionary_Key = docBarSomeDictionaryItem.Key,
                                    Bar = doc.Bar
                                }"
                        }
                    }));

                    RavenQueryStatistics stats;

                    var result = session.Query<Foo>()
                        .Where(x =>
                               x.Bar.SomeDictionary.Any(y => y.Key == "KeyOne" && y.Value == "ValueOne") ||
                               x.Bar.SomeOtherDictionary.Any(y => y.Key == "KeyFour" && y.Value == "ValueFour") ||
                               x.Bar == null)
                        .Customize(x => x.WaitForNonStaleResults())
                        .Statistics(out stats).ToList();

                    /*
                    var result2 = session.Query<Foo>("Foos/TestDynamicQueries")
                        .Where(x =>
                            x.Bar.SomeDictionary.Any(y => y.Key == "KeyOne" && y.Value == "ValueOne") ||
                                x.Bar.SomeOtherDictionary.Any(y => y.Key == "KeyFour" && y.Value == "ValueFour") ||
                                    x.Bar == null)
                                        .Customize(x => x.WaitForNonStaleResults())
                                            .Statistics(out stats).ToList();
                    */

                    Assert.Equal(stats.IndexName, "Foos/TestDynamicQueries");

                }

            }

        }


        private class Foo
        {

            public string SomeProperty { get; set; }

            public Bar Bar { get; set; }

        }

        private class Bar
        {

            public Dictionary<string, string> SomeDictionary { get; set; }
            public Dictionary<string, string> SomeOtherDictionary { get; set; }

        }
    }
}

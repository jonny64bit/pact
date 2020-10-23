using Microsoft.AspNetCore.Mvc;
using Pact.Kendo.Tests.Containers;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Pact.Kendo.Tests
{
    public class EnumerableExtensionsTests
    {
        [Fact]
        public void Kendo_CoverAll_No_Sort()
        {
            var items = new List<Basic>();
            items.Add(new Basic { Id = 1, Name = "Cat" });
            items.Add(new Basic { Id = 2, Name = "Dog" });

            var request = new KendoDataRequest
            {
                Page = 0,
                PageSize = 5,
                Skip = 0,
                Take = 5
            };

            var results = items.Kendo(request);
            Assert.True(results.Count() == 2);
        }

        [Fact]
        public void Kendo_CoverAll_Sort()
        {
            var items = new List<Basic>();
            items.Add(new Basic { Id = 1, Name = "Cat" });
            items.Add(new Basic { Id = 2, Name = "Dog" });

            var request = new KendoDataRequest
            {
                Page = 0,
                PageSize = 5,
                Skip = 0,
                Take = 5,
                Sort = new List<KendoDataRequestSort>
                {
                    new KendoDataRequestSort {Dir = "ASC", Field = "Name"}
                }
            };

            var results = items.Kendo(request);
            Assert.True(results.First().Name == "Cat");
        }

        [Fact]
        public void SoftDelete_All_Gone()
        {
            var items = new List<Basic>();
            items.Add(new Basic { Id = 1, Name = "Cat", SoftDelete = true });
            items.Add(new Basic { Id = 2, Name = "Dog", SoftDelete = true });

            var result = items.SoftDelete();

            Assert.Empty(result);
        }

        [Fact]
        public void SoftDelete_Correct_Removed()
        {
            var items = new List<Basic>();
            items.Add(new Basic { Id = 1, Name = "Cat", SoftDelete = true });
            items.Add(new Basic { Id = 2, Name = "Dog" });

            var result = items.SoftDelete();

            var resultItem = Assert.Single(result);
            Assert.True(resultItem.Id == 2);
        }

        [Fact]
        public void SoftDelete_None_Removed()
        {
            var items = new List<Basic>();
            items.Add(new Basic { Id = 1, Name = "Cat" });
            items.Add(new Basic { Id = 2, Name = "Dog" });

            var result = items.SoftDelete();

            Assert.True(result.Count() == 2);
        }

        [Fact]
        public void KendoResult_Standard()
        {
            var items = new List<Basic>();
            items.Add(new Basic { Id = 1, Name = "Cat" });
            items.Add(new Basic { Id = 2, Name = "Dog" });

            var request = new KendoDataRequest
            {
                Page = 0,
                PageSize = 5,
                Skip = 0,
                Take = 5
            };

            var result = items.KendoResult(request);

            Assert.IsType<JsonResult>(result);
            var resultValue = Assert.IsType<KendoResult<Basic>>(result.Value);
            Assert.True(resultValue.Result == "OK");
            Assert.True(resultValue.Count == 2);
            Assert.True(resultValue.Records.Count == 2);
        }

        [Theory]
        [InlineData("ASC")]
        [InlineData("DESC")]
        public void KendoResult_Skip_Take(string direction)
        {
            var items = new List<Basic>();
            items.Add(new Basic { Id = 1, Name = "Cat" });
            items.Add(new Basic { Id = 2, Name = "Dog" });
            items.Add(new Basic { Id = 3, Name = "Apple" });
            items.Add(new Basic { Id = 4, Name = "Fish" });
            items.Add(new Basic { Id = 5, Name = "Cake" });

            var request = new KendoDataRequest
            {
                Page = 0,
                PageSize = 5,
                Skip = 1,
                Take = 1,
                Sort = new List<KendoDataRequestSort>
                {
                    new KendoDataRequestSort {Dir = direction, Field = "Name"}
                }
            };

            var result = items.KendoResult(request);

            Assert.IsType<JsonResult>(result);
            var resultValue = Assert.IsType<KendoResult<Basic>>(result.Value);
            Assert.True(resultValue.Result == "OK");
            Assert.True(resultValue.Count == 5);

            var resultItem = Assert.Single(resultValue.Records);
            Assert.True(resultItem.Name == "Dog");
        }

        [Theory]
        [InlineData("dog")]
        [InlineData("doG")]
        [InlineData("Dog")]
        [InlineData("   og   ")]
        public void TextFilter_No_Attributes(string filter)
        {
            var items = new List<Basic>();
            items.Add(new Basic { Id = 1, Name = "Cat" });
            items.Add(new Basic { Id = 2, Name = "Dog" });
            items.Add(new Basic { Id = 3, Name = "Apple" });
            items.Add(new Basic { Id = 4, Name = "Fish" });
            items.Add(new Basic { Id = 5, Name = "Cake" });

            var result = items.TextFilter(filter);

            var resultItem = Assert.Single(result);
            Assert.True(resultItem.Name == "Dog");
        }

        [Fact]
        public void TextFilter_Ignore_Attribute()
        {
            var items = new List<BasicIgnore>();
            items.Add(new BasicIgnore{ Id = 1, Name = "Cat", Ignore = "Dog" });
            items.Add(new BasicIgnore { Id = 2, Name = "Dog", Ignore = "Fish" });
            items.Add(new BasicIgnore { Id = 3, Name = "Apple", Ignore = "Fish" });
            items.Add(new BasicIgnore { Id = 4, Name = "Fish", Ignore = "Dog" });
            items.Add(new BasicIgnore { Id = 5, Name = "Cake", Ignore = "Fish" });

            var result = items.TextFilter("og");

            var resultItem = Assert.Single(result);
            Assert.True(resultItem.Name == "Dog" && resultItem.Id == 2);
        }

        [Fact]
        public void TextFilter_Filter_Attribute()
        {
            var items = new List<BasicFilter>();
            items.Add(new BasicFilter { Id = 1, Name = "Cat", Filter = "Dog" });
            items.Add(new BasicFilter { Id = 2, Name = "Dog", Filter = "Fish" });
            items.Add(new BasicFilter { Id = 3, Name = "Apple", Filter = "Fish" });
            items.Add(new BasicFilter { Id = 4, Name = "Fish", Filter = "Dog" });
            items.Add(new BasicFilter { Id = 5, Name = "Cake", Filter = "Fish" });

            var result = items.TextFilter("og");

            Assert.True(result.Count() == 2);
            Assert.True(result.Any(x=>x.Id == 1) && result.Any(x => x.Id == 4));
        }
    }
}

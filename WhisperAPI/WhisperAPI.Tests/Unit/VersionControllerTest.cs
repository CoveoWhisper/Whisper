using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using WhisperAPI.Controllers;

namespace WhisperAPI.Tests.Unit
{
    [TestFixture]
    public class VersionControllerTest
    {
        [Test]
        public void When_getting_version_then_return_version()
        {
            IActionResult actionValue = new VersionController().GetVersion();
            string versionJson = actionValue.As<OkObjectResult>().Value as string;
            versionJson.Should().BeEquivalentTo("{\"version\":\"11\"}");
        }
    }
}

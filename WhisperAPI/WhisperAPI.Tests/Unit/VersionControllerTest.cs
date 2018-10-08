using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using WhisperAPI.Controllers;
using WhisperAPI.Models;

namespace WhisperAPI.Tests.Unit
{
    [TestFixture]
    public class VersionControllerTest
    {
        private VersionController _versionController;

        [Test]
        public void When_getting_version_then_return_version()
        {
            this._versionController = new VersionController();

            var result = this._versionController.GetVersion();

            var apiVersion = result.As<OkObjectResult>().Value as ApiVersion;

            apiVersion.Version.Should().BeEquivalentTo("12");
        }
    }
}

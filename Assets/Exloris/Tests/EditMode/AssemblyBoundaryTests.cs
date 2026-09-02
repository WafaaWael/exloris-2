using System.IO;
using Exloris.Core;
using NUnit.Framework;
using UnityEngine;

namespace Exloris.Tests.EditMode
{
    public sealed class AssemblyBoundaryTests
    {
        [Test]
        public void CoreHasNoUnityEngineAndNoDependencies()
        {
            var definition = Read("Core/Exloris.Core.asmdef");

            Assert.That(CoreAssemblyMarker.Name, Is.EqualTo("Exloris.Core"));
            Assert.That(definition, Does.Contain("\"references\": []"));
            Assert.That(definition, Does.Contain("\"noEngineReferences\": true"));
        }

        [TestCase("Runtime/Exloris.Runtime.asmdef")]
        [TestCase("Content/Exloris.Content.asmdef")]
        public void ProductionAdaptersDependOnlyOnCore(string relativePath)
        {
            var definition = Read(relativePath);

            Assert.That(definition, Does.Contain("\"Exloris.Core\""));
            Assert.That(definition, Does.Not.Contain("\"Exloris.Runtime\",\n    \"Exloris.Content\""));
        }

        private static string Read(string relativePath)
        {
            var path = Path.Combine(Application.dataPath, "Exloris", relativePath);
            Assert.That(File.Exists(path), Is.True, $"Missing assembly definition: {relativePath}");
            return File.ReadAllText(path);
        }
    }
}

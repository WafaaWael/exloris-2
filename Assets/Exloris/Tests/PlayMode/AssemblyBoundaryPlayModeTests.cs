using Exloris.Content;
using Exloris.Core;
using Exloris.Runtime;
using NUnit.Framework;

namespace Exloris.Tests.PlayMode
{
    public sealed class AssemblyBoundaryPlayModeTests
    {
        [Test]
        public void DeclaredProductionAssembliesResolveTogether()
        {
            Assert.That(RuntimeAssemblyMarker.Dependency, Is.EqualTo(CoreAssemblyMarker.Name));
            Assert.That(ContentAssemblyMarker.Dependency, Is.EqualTo(CoreAssemblyMarker.Name));
        }
    }
}

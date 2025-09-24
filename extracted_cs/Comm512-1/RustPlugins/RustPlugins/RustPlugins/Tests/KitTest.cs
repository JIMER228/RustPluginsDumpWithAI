using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Oxide.Plugins;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;

namespace RustPlugins.Tests
{
    [TestFixture]
    public class KitTest
    {
        private IFixture _fixture { get; set; }
        private Kits _sut;

        [SetUp]
        public void Setup()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());

            _sut = _fixture.CreateAnonymous<Kits>();
        }

        [Test]
        public void Test()
        {
            //_sut.GiveItem();
        }

    }
}

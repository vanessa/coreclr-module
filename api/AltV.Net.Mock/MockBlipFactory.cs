using System;
using AltV.Net.Elements.Entities;

namespace AltV.Net.Mock
{
    public class MockBlipFactory<TEntity> : IBaseObjectFactory<IBlip> where TEntity : IBlip
    {
        private readonly IBaseObjectFactory<IBlip> blipFactory;

        public MockBlipFactory(IBaseObjectFactory<IBlip> blipFactory)
        {
            this.blipFactory = blipFactory;
        }

        public IBlip Create(ICore core, IntPtr entityPointer)
        {
            return MockDecorator<TEntity, IBlip>.Create((TEntity) blipFactory.Create(core, entityPointer),
                new MockBlip(core, entityPointer));
        }
    }
}
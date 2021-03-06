using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
#if !NET451 && !NET452
using System.Threading;
#else
using HandlebarsDotNet.Polyfills;
#endif
using HandlebarsDotNet.Collections;
using HandlebarsDotNet.IO;
using HandlebarsDotNet.ObjectDescriptors;
using HandlebarsDotNet.PathStructure;
using HandlebarsDotNet.Pools;

namespace HandlebarsDotNet.Runtime
{
    public sealed class AmbientContext : IDisposable
    {
        private static readonly InternalObjectPool<AmbientContext, Policy> Pool = new InternalObjectPool<AmbientContext, Policy>(new Policy()); 
        
        private static readonly AsyncLocal<ImmutableStack<AmbientContext>> Local = new AsyncLocal<ImmutableStack<AmbientContext>>();

        public static AmbientContext Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Local.Value.Peek();
        }

        public static AmbientContext Create(
            PathInfoStore pathInfoStore = null, 
            ChainSegmentStore chainSegmentStore = null, 
            UndefinedBindingResultCache undefinedBindingResultCache = null,
            FormatterProvider formatterProvider = null,
            ObjectDescriptorFactory descriptorFactory = null
        )
        {
            var ambientContext = Pool.Get();
            
            ambientContext.PathInfoStore = pathInfoStore ?? new PathInfoStore();
            ambientContext.ChainSegmentStore = chainSegmentStore ?? new ChainSegmentStore();
            ambientContext.UndefinedBindingResultCache = undefinedBindingResultCache ?? new UndefinedBindingResultCache();
            ambientContext.FormatterProvider = formatterProvider ?? new FormatterProvider();
            ambientContext.ObjectDescriptorFactory = descriptorFactory ?? new ObjectDescriptorFactory();

            return ambientContext;
        }

        public static AmbientContext Create(
            AmbientContext context,
            PathInfoStore pathInfoStore = null, 
            ChainSegmentStore chainSegmentStore = null, 
            UndefinedBindingResultCache undefinedBindingResultCache = null,
            FormatterProvider formatterProvider = null,
            ObjectDescriptorFactory descriptorFactory = null
        )
        {
            var ambientContext = Pool.Get();
            
            ambientContext.PathInfoStore = pathInfoStore ?? context.PathInfoStore;
            ambientContext.ChainSegmentStore = chainSegmentStore ?? context.ChainSegmentStore;
            ambientContext.UndefinedBindingResultCache = undefinedBindingResultCache ?? context.UndefinedBindingResultCache;
            ambientContext.FormatterProvider = (formatterProvider ?? new FormatterProvider()).Append(context.FormatterProvider);
            ambientContext.ObjectDescriptorFactory = (descriptorFactory ?? new ObjectDescriptorFactory()).Append(context.ObjectDescriptorFactory);

            return ambientContext;
        }

        public static DisposableContainer Use(AmbientContext ambientContext)
        {
            Local.Value = Local.Value.Push(ambientContext);
            
            return new DisposableContainer(() =>
            {
                Local.Value = Local.Value.Pop(out _);
            });
        }
        
        private AmbientContext()
        {
        }
        
        public PathInfoStore PathInfoStore { get; private set; }
        
        public ChainSegmentStore ChainSegmentStore { get; private set; }
        
        public UndefinedBindingResultCache UndefinedBindingResultCache { get; private set; }
        
        public FormatterProvider FormatterProvider { get; private set; }
        
        public ObjectDescriptorFactory ObjectDescriptorFactory { get; private set; }
        
        public Dictionary<string, object> Bag { get; } = new Dictionary<string, object>();
        
        private struct Policy : IInternalObjectPoolPolicy<AmbientContext>
        {
            public AmbientContext Create()
            {
                return new AmbientContext();
            }

            public bool Return(AmbientContext item)
            {
                item.PathInfoStore = null;
                item.ChainSegmentStore = null;
                item.UndefinedBindingResultCache = null;
                item.FormatterProvider = null;
                item.ObjectDescriptorFactory = null;
                item.Bag.Clear();

                return true;
            }
        }

        public void Dispose() => Pool.Return(this);
    }
}
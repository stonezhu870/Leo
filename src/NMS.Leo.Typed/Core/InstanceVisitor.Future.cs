﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using NMS.Leo.Typed.Core.Loop;
using NMS.Leo.Typed.Core.Members;
using NMS.Leo.Typed.Core.Repeat;
using NMS.Leo.Typed.Core.Select;

namespace NMS.Leo.Typed.Core
{
    internal class FutureInstanceVisitor : ILeoVisitor, ILeoGetter, ILeoSetter
    {
        private readonly DictBase _handler;
        private readonly Type _sourceType;
        private readonly AlgorithmKind _algorithmKind;

        private Lazy<LeoMemberHandler> _lazyMemberHandler;

        protected HistoricalContext NormalHistoricalContext { get; set; }

        public FutureInstanceVisitor(DictBase handler, Type sourceType, AlgorithmKind kind, bool repeatable,
            IDictionary<string, object> initialValues = null)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _sourceType = sourceType ?? throw new ArgumentNullException(nameof(sourceType));
            _algorithmKind = kind;

            _handler.New();

            NormalHistoricalContext = repeatable
                ? new HistoricalContext(sourceType, kind)
                : null;

            _lazyMemberHandler = new Lazy<LeoMemberHandler>(() => new LeoMemberHandler(_handler, _sourceType));

            if (initialValues != null)
                SetValue(initialValues);
        }

        public Type SourceType => _sourceType;

        public bool IsStatic => false;

        public AlgorithmKind AlgorithmKind => _algorithmKind;

        public object Instance => _handler.GetInstance();

        public void SetValue(string name, object value)
        {
            NormalHistoricalContext?.RegisterOperation(c => c[name] = value);
            _handler[name] = value;
        }

        public void SetValue<TObj>(Expression<Func<TObj, object>> expression, object value)
        {
            if (expression is null)
                return;

            var name = PropertySelector.GetPropertyName(expression);

            NormalHistoricalContext?.RegisterOperation(c => c[name] = value);
            _handler[name] = value;
        }

        public void SetValue<TObj, TValue>(Expression<Func<TObj, TValue>> expression, TValue value)
        {
            if (expression is null)
                return;

            var name = PropertySelector.GetPropertyName(expression);

            NormalHistoricalContext?.RegisterOperation(c => c[name] = value);
            _handler[name] = value;
        }

        public void SetValue(IDictionary<string, object> keyValueCollections)
        {
            if (keyValueCollections is null)
                throw new ArgumentNullException(nameof(keyValueCollections));
            foreach (var keyValue in keyValueCollections)
                SetValue(keyValue.Key, keyValue.Value);
        }

        public object GetValue(string name)
        {
            return _handler[name];
        }

        public TValue GetValue<TValue>(string name)
        {
            return _handler.Get<TValue>(name);
        }

        public object GetValue<TObj>(Expression<Func<TObj, object>> expression)
        {
            if (expression is null)
                throw new ArgumentNullException(nameof(expression));

            var name = PropertySelector.GetPropertyName(expression);

            return _handler[name];
        }

        public TValue GetValue<TObj, TValue>(Expression<Func<TObj, TValue>> expression)
        {
            if (expression is null)
                throw new ArgumentNullException(nameof(expression));

            var name = PropertySelector.GetPropertyName(expression);

            return _handler.Get<TValue>(name);
        }

        public object this[string name]
        {
            get => GetValue(name);
            set => SetValue(name, value);
        }

        public bool TryRepeat(out object result)
        {
            result = default;
            if (IsStatic) return false;
            if (NormalHistoricalContext is null) return false;
            result = NormalHistoricalContext.Repeat();
            return true;
        }

        public bool TryRepeat(object instance, out object result)
        {
            result = default;
            if (IsStatic) return false;
            if (NormalHistoricalContext is null) return false;
            result = NormalHistoricalContext.Repeat(instance);
            return true;
        }

        public bool TryRepeat(IDictionary<string, object> keyValueCollections, out object result)
        {
            result = default;
            if (IsStatic) return false;
            if (NormalHistoricalContext is null) return false;
            result = NormalHistoricalContext.Repeat(keyValueCollections);
            return true;
        }

        public ILeoRepeater ForRepeat()
        {
            if (IsStatic) return new EmptyRepeater(_sourceType);
            if (NormalHistoricalContext is null) return new EmptyRepeater(_sourceType);
            return new LeoRepeater(NormalHistoricalContext);
        }

        public IEnumerable<string> GetMemberNames() => _lazyMemberHandler.Value.GetNames();

        public LeoMember GetMember(string name) => _lazyMemberHandler.Value.GetMember(name);

        public ILeoLooper ForEach(Action<string, object, LeoMember> loopAct)
        {
            return new LeoLooper(this, _lazyMemberHandler, loopAct);
        }

        public ILeoLooper ForEach(Action<string, object> loopAct)
        {
            return new LeoLooper(this, _lazyMemberHandler, loopAct);
        }

        public ILeoLooper ForEach(Action<LeoLoopContext> loopAct)
        {
            return new LeoLooper(this, _lazyMemberHandler, loopAct);
        }

        public ILeoSelector<TVal> Select<TVal>(Func<string, object, LeoMember, TVal> loopFunc)
        {
            return new LeoSelector<TVal>(this, _lazyMemberHandler, loopFunc);
        }

        public ILeoSelector<TVal> Select<TVal>(Func<string, object, TVal> loopFunc)
        {
            return new LeoSelector<TVal>(this, _lazyMemberHandler, loopFunc);
        }

        public ILeoSelector<TVal> Select<TVal>(Func<LeoLoopContext, TVal> loopFunc)
        {
            return new LeoSelector<TVal>(this, _lazyMemberHandler, loopFunc);
        }

        public Dictionary<string, object> ToDictionary()
        {
            var val = new Dictionary<string, object>();
            foreach (var name in _lazyMemberHandler.Value.GetNames())
                val[name] = _handler[name];
            return val;
        }

        public bool Contains(string name) => _handler.Contains(name);
    }
}
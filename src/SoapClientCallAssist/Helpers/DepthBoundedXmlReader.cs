// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-10 20:40
//
//  Last Modified By : RzR
//  Last Modified On : 2026-09-10 22:10
//  ***********************************************************************
//  <copyright file="DepthBoundedXmlReader.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using SoapClientCallAssist.Exceptions;
using System;
using System.Xml;

#endregion

namespace SoapClientCallAssist.Helpers
{
    /// <summary>
    ///     An <see cref="XmlReader" /> that forwards every call to an inner reader and refuses to read
    ///     an element nested at or beyond a fixed depth.
    /// </summary>
    internal sealed class DepthBoundedXmlReader : XmlReader
    {
        /// <summary>
        ///     The reader every call is forwarded to.
        /// </summary>
        private readonly XmlReader _inner;

        /// <summary>
        ///     The first element depth that is refused.
        /// </summary>
        private readonly int _maxDepth;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DepthBoundedXmlReader" /> class.
        /// </summary>
        /// <param name="inner">The reader to forward to. It is closed when this reader is closed.</param>
        /// <param name="maxDepth">The lowest element depth that is refused.</param>
        internal DepthBoundedXmlReader(XmlReader inner, int maxDepth)
        {
            _inner = inner;
            _maxDepth = maxDepth;
        }

        /// <inheritdoc />
        public override int AttributeCount => _inner.AttributeCount;

        /// <inheritdoc />
        public override string BaseURI => _inner.BaseURI;

        /// <inheritdoc />
        public override int Depth => _inner.Depth;

        /// <inheritdoc />
        public override bool EOF => _inner.EOF;

        /// <inheritdoc />
        public override bool HasValue => _inner.HasValue;

        /// <inheritdoc />
        public override bool IsDefault => _inner.IsDefault;

        /// <inheritdoc />
        public override bool IsEmptyElement => _inner.IsEmptyElement;

        /// <inheritdoc />
        public override string LocalName => _inner.LocalName;

        /// <inheritdoc />
        public override string NamespaceURI => _inner.NamespaceURI;

        /// <inheritdoc />
        public override XmlNameTable NameTable => _inner.NameTable;

        /// <inheritdoc />
        public override XmlNodeType NodeType => _inner.NodeType;

        /// <inheritdoc />
        public override string Prefix => _inner.Prefix;

        /// <inheritdoc />
        public override ReadState ReadState => _inner.ReadState;

        /// <inheritdoc />
        public override XmlReaderSettings Settings => _inner.Settings;

        /// <inheritdoc />
        public override string Value => _inner.Value;

        /// <inheritdoc />
        public override string XmlLang => _inner.XmlLang;

        /// <inheritdoc />
        public override XmlSpace XmlSpace => _inner.XmlSpace;

        /// <inheritdoc />
        public override string GetAttribute(int i) => _inner.GetAttribute(i);

        /// <inheritdoc />
        public override string GetAttribute(string name) 
            => _inner.GetAttribute(name);

        /// <inheritdoc />
        public override string GetAttribute(string name, string namespaceURI) 
            => _inner.GetAttribute(name, namespaceURI);

        /// <inheritdoc />
        public override string LookupNamespace(string prefix)
            => _inner.LookupNamespace(prefix);

        /// <inheritdoc />
        public override bool MoveToAttribute(string name) 
            => _inner.MoveToAttribute(name);

        /// <inheritdoc />
        public override bool MoveToAttribute(string name, string ns) 
            => _inner.MoveToAttribute(name, ns);

        /// <inheritdoc />
        public override bool MoveToElement() => _inner.MoveToElement();

        /// <inheritdoc />
        public override bool MoveToFirstAttribute() => _inner.MoveToFirstAttribute();

        /// <inheritdoc />
        public override bool MoveToNextAttribute() => _inner.MoveToNextAttribute();

        /// <inheritdoc />
        public override bool ReadAttributeValue() => _inner.ReadAttributeValue();

        /// <inheritdoc />
        public override void ResolveEntity() => _inner.ResolveEntity();

        /// <inheritdoc />
        public override void Close() => _inner.Close();

        /// <summary>
        ///     Reads the next node, refusing an element nested at or beyond the bound.
        /// </summary>
        /// <exception cref="XmlDepthExceededException">
        ///     The next element nests at or beyond the bound.
        /// </exception>
        /// <returns>
        ///     True if the next node was read successfully; false if there are no more nodes to read.
        /// </returns>
        public override bool Read()
        {
            var read = _inner.Read();

            if (read && _inner.NodeType == XmlNodeType.Element && _inner.Depth >= _maxDepth)
                throw new XmlDepthExceededException(_maxDepth);

            return read;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                ((IDisposable)_inner).Dispose();

            base.Dispose(disposing);
        }
    }
}

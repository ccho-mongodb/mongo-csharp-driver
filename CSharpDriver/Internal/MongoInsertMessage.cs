﻿/* Copyright 2010 10gen Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using MongoDB.BsonLibrary;
using MongoDB.BsonLibrary.IO;
using MongoDB.BsonLibrary.Serialization;

namespace MongoDB.CSharpDriver.Internal {
    internal class MongoInsertMessage : MongoRequestMessage {
        #region private fields
        private string collectionFullName;
        private int firstDocumentStartPosition;
        private int lastDocumentStartPosition;
        #endregion

        #region constructors
        internal MongoInsertMessage(
            string collectionFullName
        )
            : base(MessageOpcode.Insert) {
            this.collectionFullName = collectionFullName;
        }
        #endregion

        #region internal methods
        internal void AddDocument<I>(
            I document
        ) {
            lastDocumentStartPosition = buffer.Position;
            var bsonWriter = BsonWriter.Create(buffer);
            BsonSerializer.Serialize(bsonWriter, document, true); // serializeIdFirst
            BackpatchMessageLength();
        }

        internal byte[] RemoveLastDocument() {
            var lastDocumentLength = (int) (buffer.Position - lastDocumentStartPosition);
            var lastDocument = new byte[lastDocumentLength];
            buffer.CopyTo(lastDocumentStartPosition, lastDocument, 0, lastDocumentLength);
            buffer.Position = lastDocumentStartPosition;
            BackpatchMessageLength();

            return lastDocument;
        }

        internal void ResetBatch(
            byte[] lastDocument // as returned by RemoveLastDocument
        ) {
            buffer.Position = firstDocumentStartPosition;
            buffer.WriteByteArray(lastDocument);
            BackpatchMessageLength();
        }
        #endregion

        #region protected methods
        protected override void WriteBody() {
            buffer.WriteInt32(0); // reserved
            buffer.WriteCString(collectionFullName);
            firstDocumentStartPosition = buffer.Position;
            // documents to be added later by calling AddDocument
        }
        #endregion
    }
}

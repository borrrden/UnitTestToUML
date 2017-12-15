// 
//  TranslationDictionary.cs
// 
//  Author:
//   Jim Borden  <jim.borden@couchbase.com>
// 
//  Copyright (c) 2017 Couchbase, Inc All rights reserved.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//  http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
// 

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnitTestToUML
{
    public sealed class TranslationDictionary : IReadOnlyDictionary<string, string>
    {
        #region Variables

        private readonly IDictionary<string, string>[] _translationUnits;

        #endregion

        #region Properties

        public int Count => _translationUnits[0].Count;

        public string this[string key]
        {
            get {
                var retVal = default(string);
                if (!TryGetValue(key, out retVal)) {
                    return null;
                }

                return retVal;
            }
        }

        public IEnumerable<string> Keys => _translationUnits[0].Keys;

        public IEnumerable<string> Values
        {
            get {
                foreach (var key in Keys) {
                    yield return this[key];
                }
            }
        }

        #endregion

        #region Constructors

        public TranslationDictionary(params IDictionary<string, string>[] translationUnits)
        {
            _translationUnits = translationUnits;
        }

        #endregion

        #region IEnumerable

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region IEnumerable<KeyValuePair<string,string>>

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => new Enumerator(this);

        #endregion

        #region IReadOnlyDictionary<string,string>

        public bool ContainsKey(string key)
        {
            return TryGetValue(key, out string tmp);
        }

        public bool TryGetValue(string key, out string value)
        {
            var next = key;
            value = null;
            foreach (var unit in _translationUnits) {
                if (!unit.TryGetValue(next, out value)) {
                    if (next == key) {
                        return false;
                    }

                    value = next;
                    return true;
                }

                next = value;
            }

            return true;
        }

        #endregion

        #region Nested

        private class Enumerator : IEnumerator<KeyValuePair<string, string>>
        {
            #region Variables

            private readonly string[] _keys;
            private readonly TranslationDictionary _parent;
            private int _index = 0;

            #endregion

            #region Properties

            object IEnumerator.Current => Current;

            public KeyValuePair<string, string> Current => new KeyValuePair<string, string>(_keys[_index], _parent[_keys[_index]]);

            #endregion

            #region Constructors

            public Enumerator(TranslationDictionary parent)
            {
                _keys = parent.Keys.ToArray();
                _parent = parent;
            }

            #endregion

            #region IDisposable

            public void Dispose()
            {
                
            }

            #endregion

            #region IEnumerator

            public bool MoveNext()
            {
                if (_index == _keys.Length - 1) {
                    return false;
                }

                _index++;
                return true;
            }

            public void Reset() => throw new NotSupportedException();

            #endregion
        }

        #endregion
    }
}
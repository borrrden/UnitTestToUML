// 
//  Config.cs
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

using System.Collections.Generic;

namespace UnitTestToUML
{
    public sealed class Config
    {
        public Dictionary<string, string> AppleFunctionMap { get; set; } = new Dictionary<string, string>();

        public Dictionary<string, string> FromAppleFunctionMap { get; set; } = new Dictionary<string, string>();

        public Dictionary<string, string> JavaFunctionMap { get; set; } = new Dictionary<string, string>();

        public Dictionary<string, string> FromJavaFunctionMap { get; set; } = new Dictionary<string, string>();

        public IReadOnlyList<string> CSharpSkip { get; set; } = new List<string>();

        public IReadOnlyList<string> AppleSkip { get; set; } = new List<string>();

        public IReadOnlyList<string> JavaSkip { get; set; } = new List<string>();

        public IReadOnlyList<string> SkipFiles { get; set; } = new List<string>();

        public void Populate()
        {
            foreach (var pair in AppleFunctionMap) {
                FromAppleFunctionMap[pair.Value] = pair.Key;
            }

            foreach (var pair in JavaFunctionMap) {
                FromJavaFunctionMap[pair.Value] = pair.Key;
            }
        }
    }
}
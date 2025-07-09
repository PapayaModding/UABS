using System;
using System.Collections.Generic;
using System.Linq;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Wrapper.Json;

namespace UABS.Assets.Script.Reader
{
    public class ReadSearchInfo
    {
        private readonly FindDeriveInfoInCache _findDeriveInfoInCache;

        public ReadSearchInfo(IJsonSerializer jsonSerializer)
        {
            _findDeriveInfoInCache = new(jsonSerializer);
        }

        public List<DeriveInfo> ReadInfoFor(string fromCache,
                                            string searchKeywords,
                                            string excludeKeywords,
                                            bool exactMatch=false)
        {
            List<string> sKeys = SplitByComma(searchKeywords);
            List<string> eKeys = SplitByComma(excludeKeywords);
            return _findDeriveInfoInCache.FindInCacheBySearchOptions(fromCache, sKeys, eKeys, exactMatch);
        }

        public static List<string> SplitByComma(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new List<string>();

            // Split by both English (,) and Chinese (，) commas
            var parts = input.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries);

            // Trim spaces and remove empty results
            return parts
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList();
        }
    }
}
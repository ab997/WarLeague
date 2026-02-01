using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Repositories;

namespace WarLeague.Core.Domain.Services
{
    public class FormatService
    {
        private readonly FormatRepository _formatRepository;
        public FormatService(FormatRepository formatRepo)
        {
            _formatRepository = formatRepo;
        }
        /// <summary>
        /// returns newly created Format or null if format with given name already exists
        /// </summary>
        public async Task<Format?> CreateFormatAsync(string formatName)
        {
            Format? existing = await _formatRepository.GetByNameAsync(formatName);

            if (existing != null)
            {
                return null;
            }

            Format format = new Format
            {
                Name = formatName
            };

            await _formatRepository.AddAsync(format);

            return format;
        }
        /// <summary>
        /// return deleted format of null if format with given name does not exist
        /// </summary>
        public async Task<Format?> DeleteFormatAsync(string formatName)
        {
            var format = await _formatRepository.GetByNameAsync(formatName);
            if (format == null)
            {
                return null;
            }

            await _formatRepository.DeleteAsync(format);
            return format;
        }
        /// <summary>
        /// return updated format or null if format with given name does not exist
        /// </summary>
        /// <param name="formatName"></param>
        /// <param name="rulesJson"></param>
        /// <returns></returns>
        public async Task<Format?> UpdateFormatRulesAsync(string formatName, string rulesJson)
        {
            var format = await _formatRepository.GetByNameAsync(formatName);

            if (format == null)
            {
                return null;
            }

            format.Rules = rulesJson;

            await _formatRepository.UpdateAsync(format);

            return format;
        }
    }
}

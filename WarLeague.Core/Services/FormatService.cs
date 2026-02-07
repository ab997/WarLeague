using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Data;
using WarLeague.Data.Entities;
using WarLeague.Core.Model;
using WarLeague.Core.Repositories;

namespace WarLeague.Core.Services
{
    public class FormatService
    {
        private readonly FormatRepository _formatRepository;
        private readonly WarLeagueDbContext _context;
        public FormatService(FormatRepository formatRepo, WarLeagueDbContext context)
        {
            _formatRepository = formatRepo;
            _context = context;
        }

        public async Task<BaseResult> CreateFormatAsync(string formatName)
        {
            Format? existing = await _formatRepository.GetByNameAsync(formatName);

            if (existing != null)
            {
                return new BaseResult(false, $"Format with name {formatName} already exists.");
            }

            Format format = new Format
            {
                Name = formatName
            };

            await _formatRepository.AddAsync(format);

            return new BaseResult(true, $"Format {formatName} created.");
        }

        public async Task<BaseResult> DeleteFormatAsync(string formatName)
        {
            var format = await _formatRepository.GetByNameAsync(formatName);
            if (format == null)
            {
                return new BaseResult(false, $"Format with name {formatName} not found.");
            }

            await _formatRepository.DeleteAsync(format);
            return new BaseResult(true, $"Format '{formatName}' deleted.");
        }

        public async Task<Format?> GetFormatAsync(string formatName)
        {
            return await _formatRepository.GetByNameAsync(formatName);
        }

        public async Task<BaseResult> SetSingleFormatModeAsync(int formatId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            // first disable single format mode for all formats

            var formats = await _formatRepository.GetAllAsync();

            foreach (var format in formats)
            {
                format.SingleFormatMode = false;
                await _formatRepository.UpdateAsync(format);
            }

            // then enable single format mode for the given format
            var targetFormat = await _formatRepository.GetByIdAsync(formatId);
            targetFormat.SingleFormatMode = true;
            await _formatRepository.UpdateAsync(targetFormat);

            await transaction.CommitAsync();

            return new BaseResult(true, "Done.");
        }

        public async Task<BaseResult> UpdateFormatRulesAsync(string formatName, string rulesJson)
        {
            var format = await _formatRepository.GetByNameAsync(formatName);

            if (format == null)
            {
                return new BaseResult(false, $"Format with name {formatName} not found.");
            }

            format.Rules = rulesJson;

            await _formatRepository.UpdateAsync(format);

            return new BaseResult(true, $"Rules updated for format '{formatName}'.");
        }
    }
}

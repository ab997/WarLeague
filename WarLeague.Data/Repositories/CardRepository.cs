using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Data.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace WarLeague.Data.Repositories
{
    public class CardRepository
    {
        private readonly WarLeagueDbContext _db;

        public CardRepository(WarLeagueDbContext db)
        {
            _db = db;
        }

        public async Task<Card?> GetByYgoproIdAsync(string ygoproId, CancellationToken ct = default)
        {
            return await _db.Cards.FirstOrDefaultAsync(c => c.YgoproId == ygoproId, ct);
        }

        public async Task UpdateAsync(Card card, CancellationToken ct = default)
        {
            // card is already tracked since it came from this context via GetByYgoproIdAsync;
            // EF will pick up the mutated properties on SaveChanges.
            await _db.SaveChangesAsync(ct);
        }

        public async Task AddAsync(Card card, CancellationToken ct = default)
        {
            _db.Cards.Add(card);
            await _db.SaveChangesAsync(ct);
        }

        public async Task AddRangeAsync(IEnumerable<Card> cards, CancellationToken ct = default)
        {
            _db.Cards.AddRange(cards);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<List<Card>> GetAllFilteredAsync(List<string> ygoProIds)
        {
            return await _db.Cards.Where(x => ygoProIds.Contains(x.YgoproId)).ToListAsync();
        }
    }
}

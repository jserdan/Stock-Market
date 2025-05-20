using System;
using System.Collections.Generic;
using System.Linq;

namespace danserdan.Models
{
    public class PaginatedList<T> : List<T>
    {
        public int PageIndex { get; private set; }
        public int TotalPages { get; private set; }
        public int TotalItems { get; private set; }
        public int PageSize { get; private set; }
        public int TotalCount => TotalItems; // Alias for TotalItems for better readability in views
        
        // Calculate the index of the first item on the current page (1-based index for display)
        public int FirstItemIndex => (PageIndex - 1) * PageSize + 1;
        
        // Calculate the index of the last item on the current page
        public int LastItemIndex => Math.Min(PageIndex * PageSize, TotalItems);

        // Number of page links to show in pagination (excluding Previous/Next)
        public int ChunkSize { get; private set; } = 3;

        // Get the range of page numbers to display in the pagination
        public IEnumerable<int> GetVisiblePageNumbers()
        {
            // Calculate the start and end of the chunk centered around the current page
            int halfChunk = ChunkSize / 2;
            int chunkStart = Math.Max(1, PageIndex - halfChunk);
            int chunkEnd = Math.Min(TotalPages, chunkStart + ChunkSize - 1);
            
            // Adjust the start if we're near the end to ensure we always show ChunkSize pages if possible
            if (chunkEnd - chunkStart + 1 < ChunkSize && chunkStart > 1)
            {
                chunkStart = Math.Max(1, chunkEnd - ChunkSize + 1);
            }
            
            return Enumerable.Range(chunkStart, chunkEnd - chunkStart + 1);
        }

        // Check if we should show the first page link separately
        public bool ShowFirstPage => GetVisiblePageNumbers().First() > 1;

        // Check if we should show the last page link separately
        public bool ShowLastPage => GetVisiblePageNumbers().Last() < TotalPages;

        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize, int chunkSize = 3)
        {
            PageIndex = pageIndex;
            PageSize = pageSize;
            TotalItems = count;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            ChunkSize = chunkSize;

            this.AddRange(items);
        }

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public static PaginatedList<T> Create(IQueryable<T> source, int pageIndex, int pageSize, int chunkSize = 3)
        {
            var count = source.Count();
            var items = source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
            return new PaginatedList<T>(items, count, pageIndex, pageSize, chunkSize);
        }
    }
}

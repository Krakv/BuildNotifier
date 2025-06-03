using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildNotifier.Data.Models.Pagination
{
    /// <summary>
    /// Класс для управления состоянием пагинации списка планов.
    /// Хранит информацию о текущей странице, размере страницы, общем количестве страниц,
    /// а также списки планов для отображения.
    /// </summary>
    public class PaginationState
    {
        /// <summary>
        /// Полный список всех названий планов, доступных для пагинации.
        /// </summary>
        public List<string> AllPlanNames { get; set; } = new List<string>();

        /// <summary>
        /// Текущая страница.
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// Количество элементов на одной странице.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Общее количество страниц.
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Словарь планов для текущей страницы.
        /// Ключ — символ или идентификатор, значение — название плана.
        /// Используется для быстрого доступа к данным текущей страницы.
        /// </summary>
        public Dictionary<char, string> CurrentPagePlans { get; set; } = new();
    }
}

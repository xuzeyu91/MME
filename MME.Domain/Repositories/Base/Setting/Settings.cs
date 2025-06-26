using System;
using System.Collections.Generic;
using SqlSugar;

namespace MME.Repositories.Demo
{
    /// <summary>
    /// Setting
    /// </summary>
    [SugarTable("Settings")]
    public class Settings
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Name { get; set; }
        public string Value { get; set; }
    }
} 
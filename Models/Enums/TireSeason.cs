// File: Models/Enums/TireSeason.cs
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.Enums;

public enum TireSeason
{
    [Display(Name = "Yazlık")]
    Summer = 1,

    [Display(Name = "Kışlık")]
    Winter = 2,

    [Display(Name = "4 Mevsim")]
    AllSeason = 3
}

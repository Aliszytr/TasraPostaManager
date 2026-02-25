using System;
using System.ComponentModel.DataAnnotations;

namespace TasraPostaManager.Models;

public class BarcodePoolItem
{
    public int Id { get; set; }

    [Required]
    [MaxLength(64)]
    public string Barcode { get; set; } = string.Empty;

    public bool IsUsed { get; set; }

    public BarcodePoolStatus Status { get; set; } = BarcodePoolStatus.Available;

    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UsedAt { get; set; }

    [MaxLength(64)]
    public string? BatchId { get; set; }

    [MaxLength(128)]
    public string? Source { get; set; }

    [MaxLength(64)]
    public string? UsedByRecordKey { get; set; }
}

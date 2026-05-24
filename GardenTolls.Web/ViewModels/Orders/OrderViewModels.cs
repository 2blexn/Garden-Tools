using System.ComponentModel.DataAnnotations;

namespace GardenTolls.Web.ViewModels.Orders;

public class CheckoutViewModel
{
    [Required]
    public string ShippingAddress { get; set; } = string.Empty;

    [Required]
    public string ShippingCity { get; set; } = string.Empty;

    [Required]
    public string ShippingCountry { get; set; } = "Україна";

    public string? ShippingPostalCode { get; set; }

    [Required]
    public string PaymentMethod { get; set; } = "Card";

    public string? Notes { get; set; }
    public decimal TotalAmount { get; set; }
    public List<CheckoutLineViewModel> Lines { get; set; } = [];
}

public class CheckoutLineViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageBase64 { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class PaymentViewModel
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }

    [Required]
    public string CardNumber { get; set; } = string.Empty;

    [Required]
    public string CardHolder { get; set; } = string.Empty;

    [Required]
    public string Expiry { get; set; } = string.Empty;

    [Required]
    public string Cvv { get; set; } = string.Empty;
}

public class OrderHistoryViewModel
{
    public List<OrderSummaryViewModel> Orders { get; set; } = [];
}

public class OrderSummaryViewModel
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
}

public class OrderDetailsViewModel
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingCountry { get; set; } = string.Empty;
    public string? ShippingPostalCode { get; set; }
    public List<CheckoutLineViewModel> Lines { get; set; } = [];
}

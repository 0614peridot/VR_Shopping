using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class CS_CheckoutTests
{
    [Test]
    public void Clear_EmptiesEntriesAndTotal_AndNotifiesListeners()
    {
        GameObject cartObject = new GameObject("Cart");
        GameObject productObject = new GameObject("Product");
        cartObject.SetActive(false);
        productObject.SetActive(false);

        try
        {
            CS_CartCounter cart = cartObject.AddComponent<CS_CartCounter>();
            CS_ProductItem product = productObject.AddComponent<CS_ProductItem>();
            product.Configure("Test Jacket", 125000, new Collider[0], new Renderer[0]);
            int changeCount = 0;
            cart.Changed += () => changeCount++;

            cart.AddProduct(product);
            cart.AddProduct(product);

            IReadOnlyList<CS_CartCounter.CS_CartEntry> entries = cart.Entries;
            Assert.That(entries.Count, Is.EqualTo(2));
            Assert.That(entries[0].ProductName, Is.EqualTo("Test Jacket"));
            Assert.That(cart.TotalPriceWon, Is.EqualTo(250000));

            cart.Clear();

            Assert.That(cart.Count, Is.Zero);
            Assert.That(entries.Count, Is.Zero, "The read-only view should reflect the cleared cart.");
            Assert.That(cart.TotalPriceWon, Is.Zero);
            Assert.That(changeCount, Is.EqualTo(3));
        }
        finally
        {
            Object.DestroyImmediate(productObject);
            Object.DestroyImmediate(cartObject);
        }
    }

    [Test]
    public void Entries_CannotBeMutatedThroughThePublicCollection()
    {
        GameObject cartObject = new GameObject("Cart");
        cartObject.SetActive(false);

        try
        {
            CS_CartCounter cart = cartObject.AddComponent<CS_CartCounter>();
            ICollection<CS_CartCounter.CS_CartEntry> entries =
                (ICollection<CS_CartCounter.CS_CartEntry>)cart.Entries;

            Assert.That(entries.IsReadOnly, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(cartObject);
        }
    }
}

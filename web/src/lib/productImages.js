const previewImages = {
  'DEMO75-BLK-LIN': '/assets/keyboard.jpg',
  'SF75-BLK-LIN': '/assets/q1-black.jpg',
  'TEST-CONFIG-001': '/assets/angle-1.jpg',
};

// Temporary presentation mapping until the API supplies images for each SKU.
export const productImage = product => previewImages[product.sku] || '/assets/keyboard.jpg';

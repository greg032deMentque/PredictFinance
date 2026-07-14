/** Libellé FR d'un type de produit d'éducation/PEA. Source unique partagée
 *  (client + admin) pour éviter la duplication du mapping. */
export function educationProductTypeLabel(productType: string): string {
  switch (productType) {
    case 'LifeInsurance': return 'Assurance vie';
    case 'PEA': return 'PEA';
    case 'PEL': return 'PEL';
    case 'PER': return 'PER';
    case 'General': return 'Général';
    default: return productType;
  }
}

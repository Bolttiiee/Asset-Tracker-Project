export interface Asset {
  assetID: number;
  name: string;
  category: string;
  serialNumber: string;
  condition: 'New' | 'Good' | 'Fair' | 'Poor';
  status: 'Available' | 'Checked Out' | 'Under Maintenance' | 'Decommissioned';
  assignedTo: string;
  purchaseDate: string | Date;
  dateAdded?: string | Date;
  decommissionDate?: string | null;
  value: number;
  notes?: string;
}

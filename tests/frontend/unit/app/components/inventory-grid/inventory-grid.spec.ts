import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { AssetService } from '../../../../app/services/asset';
import { UserService } from '../../../../app/services/user';
import { AdminService } from '../../../../app/services/admin';

import { InventoryGridComponent } from '../../../../app/components/inventory-grid/inventory-grid';

describe('InventoryGridComponent', () => {
  let component: InventoryGridComponent;
  let fixture: ComponentFixture<InventoryGridComponent>;
  let assignCalls: any[];
  let returnCalls: number[];
  let maintenanceCalls: Array<{ assetId: number; setMaintenance: boolean }>;
  let deleteCalls: number[];
  let restoreCalls: number[];

  beforeEach(async () => {
    assignCalls = [];
    returnCalls = [];
    maintenanceCalls = [];
    deleteCalls = [];
    restoreCalls = [];

    await TestBed.configureTestingModule({
      imports: [InventoryGridComponent],
      providers: [
        {
          provide: AssetService,
          useValue: {
            getAssets: () => of([]),
            getDashboardMetrics: () => of({}),
            assignAsset: (...args: any[]) => {
              assignCalls.push(args);
              return of({ message: 'Assigned' });
            },
            returnAsset: (assetId: number) => {
              returnCalls.push(assetId);
              return of({ message: 'Returned' });
            },
            setMaintenanceStatus: (assetId: number, setMaintenance: boolean) => {
              maintenanceCalls.push({ assetId, setMaintenance });
              return of({ message: 'Updated' });
            },
            deleteAsset: (assetId: number) => {
              deleteCalls.push(assetId);
              return of({});
            },
            restoreAsset: (assetId: number) => {
              restoreCalls.push(assetId);
              return of({ message: 'Restored' });
            },
            adminOverrideReturn: () => of({ message: 'Force return successful.' }),
          },
        },
        {
          provide: UserService,
          useValue: {
            getAllUsers: () => of([]),
          },
        },
        {
          provide: AdminService,
          useValue: {
            runOverdueReminders: () => of({ overdueCount: 0, remindersSent: 0, remindersFailed: 0, digestSent: false }),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(InventoryGridComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('filters assets by search term, status, category, and minimum value', () => {
    component.assets = [
      { assetID: 1, name: 'Dell Laptop', serialNumber: 'ABC12345', category: 'Laptop', value: 1200, status: 'Available', assignedTo: '' } as any,
      { assetID: 2, name: 'Apple Keyboard', serialNumber: 'XYZ99999', category: 'Keyboard', value: 200, status: 'Checked Out', assignedTo: '' } as any,
    ];

    component.searchTerm = 'dell';
    expect(component.filteredAssets.length).toBe(1);

    component.searchTerm = '';
    component.statusFilter = 'Checked Out';
    expect(component.filteredAssets.length).toBe(1);

    component.statusFilter = '';
    component.categoryFilter = 'Laptop';
    expect(component.filteredAssets.length).toBe(1);

    component.categoryFilter = '';
    component.minValueFilter = 500;
    expect(component.filteredAssets.length).toBe(1);
  });

  it('hides decommissioned assets from standard users', () => {
    component.userRole = 'Standard User';
    component.assets = [
      { assetID: 1, name: 'Old PC', serialNumber: 'OLD12345', category: 'Desktop', value: 100, status: 'Decommissioned', assignedTo: '' } as any,
      { assetID: 2, name: 'Dell Laptop', serialNumber: 'ABC12345', category: 'Laptop', value: 1200, status: 'Available', assignedTo: '' } as any,
    ];
    expect(component.filteredAssets.length).toBe(1);
    expect(component.filteredAssets[0].assetID).toBe(2);
  });

  it('recognizes admin and asset manager roles', () => {
    component.userRole = 'Admin';
    expect(component.isAdminOrManager()).toBe(true);

    component.userRole = 'Asset Manager';
    expect(component.isAdminOrManager()).toBe(true);

    component.userRole = 'Standard User';
    expect(component.isAdminOrManager()).toBe(false);
  });

  it('blocks checkout for missing required fields', () => {
    component.assets = [{ assetID: 1, name: 'Dell Laptop', serialNumber: 'ABC12345', status: 'Available', assignedTo: '', value: 1200 } as any];

    component.handleCheckout(1);

    expect(component.toastMessage).toContain('missing required full name');
    expect(assignCalls.length).toBe(0);
  });

  it('blocks checkout for invalid email and past return date', () => {
    component.assets = [{ assetID: 1, name: 'Dell Laptop', serialNumber: 'ABC12345', status: 'Available', assignedTo: '', value: 1200 } as any];
    component.checkoutFullName = 'Jane Doe';
    component.checkoutEmail = 'bad-email';
    component.checkoutExpectedReturnDate = component.today;
    component.handleCheckout(1);
    expect(component.toastMessage).toContain('valid email address');

    component.checkoutEmail = 'jane@company.com';
    component.checkoutExpectedReturnDate = '2000-01-01';
    component.handleCheckout(1);
    expect(component.toastMessage).toContain('in the past');
  });

  it('blocks checkout for non-available and decommissioned assets', () => {
    component.assets = [
      { assetID: 1, name: 'Dell Laptop', serialNumber: 'ABC12345', status: 'Checked Out', assignedTo: '', value: 1200 } as any,
      { assetID: 2, name: 'HP Laptop', serialNumber: 'DEF12345', status: 'Decommissioned', assignedTo: '', value: 900 } as any,
    ];

    component.handleCheckout(1);
    expect(component.toastMessage).toContain('only available assets');

    component.handleCheckout(2);
    expect(component.toastMessage).toContain('decommissioned assets');
  });

  it('successful checkout updates local asset state after confirmation', () => {
    const originalSetTimeout = globalThis.setTimeout;
    (globalThis as any).setTimeout = () => 0;
    try {
      (component as any).loadInventory = () => {};
      component.assets = [{ assetID: 1, name: 'Dell Laptop', serialNumber: 'ABC12345', status: 'Available', assignedTo: '', value: 1200 } as any];
      component.checkoutFullName = 'Jane Doe';
      component.checkoutEmail = 'jane@company.com';
      component.checkoutExpectedReturnDate = '2099-01-01';

      component.handleCheckout(1);
      component.confirmAction();

      expect(assignCalls.length).toBe(1);
      expect(component.assets[0].status).toBe('Checked Out');
      expect(component.assets[0].assignedTo).toContain('Jane Doe');
    } finally {
      globalThis.setTimeout = originalSetTimeout;
    }
  });

  it('successful return updates asset back to available after confirmation', () => {
    const originalSetTimeout = globalThis.setTimeout;
    (globalThis as any).setTimeout = () => 0;
    try {
      (component as any).loadInventory = () => {};
      component.assets = [{ assetID: 1, name: 'Dell Laptop', serialNumber: 'ABC12345', status: 'Checked Out', assignedTo: 'Jane', value: 1200 } as any];

      component.handleReturn(1);
      component.confirmAction();

      expect(returnCalls).toEqual([1]);
      expect(component.assets[0].status).toBe('Available');
      expect(component.assets[0].assignedTo).toBe('');
    } finally {
      globalThis.setTimeout = originalSetTimeout;
    }
  });

  it('toggleMaintenance updates local status correctly', () => {
    const originalSetTimeout = globalThis.setTimeout;
    (globalThis as any).setTimeout = () => 0;
    try {
      (component as any).loadInventory = () => {};
      component.assets = [{ assetID: 1, name: 'Dell Laptop', serialNumber: 'ABC12345', status: 'Available', assignedTo: '', value: 1200 } as any];

      component.toggleMaintenance(component.assets[0] as any, true);
      // Maintenance now requires confirmation before executing
      component.confirmAction();
      expect(maintenanceCalls).toEqual([{ assetId: 1, setMaintenance: true }]);
      expect(component.assets[0].status).toBe('Under Maintenance');
    } finally {
      globalThis.setTimeout = originalSetTimeout;
    }
  });

  it('decommission and restore update local asset state', () => {
    const originalSetTimeout = globalThis.setTimeout;
    (globalThis as any).setTimeout = () => 0;
    try {
      (component as any).loadInventory = () => {};
      component.assets = [{ assetID: 1, name: 'Dell Laptop', serialNumber: 'ABC12345', status: 'Available', assignedTo: 'Jane', value: 1200 } as any];

      component.decommissionAsset(component.assets[0] as any);
      component.confirmAction();
      expect(deleteCalls).toEqual([1]);
      expect(component.assets[0].status).toBe('Decommissioned');

      component.restoreAsset(component.assets[0] as any);
      component.confirmAction();
      expect(restoreCalls).toEqual([1]);
      expect(component.assets[0].status).toBe('Available');
    } finally {
      globalThis.setTimeout = originalSetTimeout;
    }
  });

  it('reverts local asset state when decommission fails', () => {
    const originalSetTimeout = globalThis.setTimeout;
    (globalThis as any).setTimeout = () => 0;
    try {
      const assetService = TestBed.inject(AssetService) as any;
      assetService.deleteAsset = (assetId: number) => throwError(() => new Error('Decommission failed'));

      (component as any).loadInventory = () => {};
      component.assets = [{ assetID: 1, name: 'Dell Laptop', serialNumber: 'ABC12345', status: 'Available', assignedTo: 'Jane', value: 1200 } as any];

      component.decommissionAsset(component.assets[0] as any);
      component.confirmAction();
      
      expect(component.assets[0].status).toBe('Available');
    } finally {
      globalThis.setTimeout = originalSetTimeout;
    }
  });

  it('runOverdueReminders opens confirmation and calls service when confirmed', () => {
    const adminService = TestBed.inject(AdminService) as any;
    const runSpy = vi.spyOn(adminService, 'runOverdueReminders');

    component.runOverdueReminders();
    expect(runSpy).not.toHaveBeenCalled();
    expect(component.confirmationModalOpen).toBe(true);

    component.confirmAction();
    expect(runSpy).toHaveBeenCalled();
  });
});





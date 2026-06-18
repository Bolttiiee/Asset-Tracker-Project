import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { CheckoutService } from '../../../../app/services/checkout';

import { MyAssetsComponent } from '../../../../app/pages/my-assets/my-assets';

describe('MyAssetsComponent', () => {
  let component: MyAssetsComponent;
  let fixture: ComponentFixture<MyAssetsComponent>;
  let myAssetsCalls: number;
  let returnCalls: number[];

  beforeEach(async () => {
    myAssetsCalls = 0;
    returnCalls = [];

    await TestBed.configureTestingModule({
      imports: [MyAssetsComponent],
      providers: [
        {
          provide: CheckoutService,
          useValue: {
            getMyAssets: () => {
              myAssetsCalls++;
              return of([]);
            },
            returnAsset: (assetId: number) => {
              returnCalls.push(assetId);
              return of({});
            },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(MyAssetsComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('loads assets on init', () => {
    myAssetsCalls = 0;

    component.ngOnInit();

    expect(myAssetsCalls).toBe(1);
    expect(component.loading).toBe(false);
  });

  it('loadMyAssets populates assets and handles errors', () => {
    const service = TestBed.inject(CheckoutService) as any;
    service.getMyAssets = () => of([{ checkoutID: 1, assetID: 2, assetName: 'Dell Laptop', category: 'Laptop', serialNumber: 'ABC12345', checkoutDate: '2026-06-01', expectedReturnDate: '2099-01-01', isOverdue: false }]);

    component.loadMyAssets();
    expect(component.assets.length).toBe(1);

    service.getMyAssets = () => throwError(() => ({ error: { message: 'Unable to load your checked-out assets.' } }));
    component.loadMyAssets();
    expect(component.errorMessage).toBe('Unable to load your checked-out assets.');
  });

  it('getDaysOverdue returns zero for future dates and positive values for overdue dates', () => {
    expect(component.getDaysOverdue('2099-01-01')).toBe(0);
    expect(component.getDaysOverdue('2000-01-01')).toBeGreaterThan(0);
  });

  it('returnAsset calls service when confirmed and reloads list', () => {
    const reloadSpy = vi.spyOn(component, 'loadMyAssets').mockImplementation(() => {});

    // Call returnAsset to open dialog
    component.returnAsset({ checkoutID: 4, assetID: 2, assetName: 'Dell Laptop', category: 'Laptop', serialNumber: 'ABC12345', checkoutDate: '2026-06-01', expectedReturnDate: '2099-01-01', isOverdue: false });
    
    // Call confirmReturn to execute the return
    component.confirmReturn();
    
    expect(returnCalls).toEqual([2]);
    expect(reloadSpy).toHaveBeenCalled();
    expect(component.successMessage).toContain('Dell Laptop');
  });

  it('returnAsset sets error message on failure', () => {
    const service = TestBed.inject(CheckoutService) as any;
    service.returnAsset = () => throwError(() => ({ error: { message: 'Return failed.' } }));

    // Call returnAsset to open dialog
    component.returnAsset({ checkoutID: 4, assetID: 2, assetName: 'Dell Laptop', category: 'Laptop', serialNumber: 'ABC12345', checkoutDate: '2026-06-01', expectedReturnDate: '2099-01-01', isOverdue: false });
    
    // Call confirmReturn to execute the return
    component.confirmReturn();
    
    expect(component.errorMessage).toBe('Return failed.');
    expect(component.returningAssetId).toBeNull();
  });
});

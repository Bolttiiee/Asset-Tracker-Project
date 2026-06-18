import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { AssetService } from '../../../../app/services/asset';

import { AssetModalComponent } from '../../../../app/components/asset-modal/asset-modal';

describe('AssetModalComponent', () => {
  let component: AssetModalComponent;
  let fixture: ComponentFixture<AssetModalComponent>;
  let createdPayloads: any[];
  let updatedPayloads: Array<{ id: number; asset: any }>;
  let emittedCount: number;

  beforeEach(async () => {
    createdPayloads = [];
    updatedPayloads = [];
    emittedCount = 0;

    await TestBed.configureTestingModule({
      imports: [AssetModalComponent],
      providers: [{
        provide: AssetService,
        useValue: {
          getAssets: () => of([]),
          createAsset: (payload: any) => {
            createdPayloads.push(payload);
            return of({});
          },
          updateAsset: (id: number, asset: any) => {
            updatedPayloads.push({ id, asset });
            return of({});
          },
        },
      }],
    }).compileComponents();

    fixture = TestBed.createComponent(AssetModalComponent);
    component = fixture.componentInstance;
    component.assetCreated.subscribe(() => emittedCount++);
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('openForCreate resets state and opens modal', () => {
    component.openForCreate();

    expect(component.mode).toBe('create');
    expect(component.isOpen).toBe(true);
    expect(component.assetForm.get('status')?.value).toBe('Available');
  });

  it('openForEdit loads asset values and switches mode', () => {
    component.openForEdit({ assetID: 3, name: 'Dell Laptop', category: 'Laptop', serialNumber: 'ABC12345', value: 1200, status: 'Available' } as any);

    expect(component.mode).toBe('edit');
    expect(component.assetForm.get('name')?.value).toBe('Dell Laptop');
    expect(component.assetForm.get('serialNumber')?.value).toBe('ABC12345');
    expect(component.assetForm.get('category')?.value).toBe('Laptop');
  });

  it('invalid submit marks fields touched and does not call create/update', () => {
    component.submitAsset();

    expect(component.assetForm.touched).toBe(true);
    expect(createdPayloads.length).toBe(0);
    expect(updatedPayloads.length).toBe(0);
  });

  it('blocks duplicate serial numbers', () => {
    const assetService = TestBed.inject(AssetService) as any;
    assetService.getAssets = () => of([{ assetID: 9, serialNumber: 'ABC12345' }]);

    component.openForCreate();
    component.assetForm.setValue({ name: 'Dell Laptop', category: 'Laptop', serialNumber: 'ABC12345', condition: 'Good', value: 1200, status: 'Available', notes: '' });
    component.submitAsset();

    expect(component.serialValidationError).toContain('Serial number already exists');
    expect(createdPayloads.length).toBe(0);
  });

  it('successful create emits assetCreated and sends normalized payload', () => {
    component.openForCreate();
    component.assetForm.setValue({ name: 'Dell Laptop', category: 'Laptop', serialNumber: 'ABC12345', condition: 'Good', value: 1200, status: 'Available', notes: '' });
    component.submitAsset();

    expect(createdPayloads.length).toBe(1);
    expect(createdPayloads[0].serialNumber).toBe('ABC12345');
    expect(emittedCount).toBe(1);
  });

  it('successful edit calls updateAsset with the asset id', () => {
    component.openForEdit({ assetID: 7, name: 'Dell Laptop', category: 'Laptop', serialNumber: 'ABC12345', condition: 'Good', value: 1200, status: 'Available', notes: '' } as any);
    component.assetForm.setValue({ name: 'Dell Laptop Pro', category: 'Laptop', serialNumber: 'ABC12345', condition: 'Good', value: 1500, status: 'Available', notes: 'Updated' });
    component.submitAsset();

    expect(updatedPayloads.length).toBe(1);
    expect(updatedPayloads[0].id).toBe(7);
    expect(updatedPayloads[0].asset.serialNumber).toBe('ABC12345');
  });
});

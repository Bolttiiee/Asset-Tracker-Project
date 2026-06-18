import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AssetService } from '../../../../app/services/asset';

import { AuditTimelineComponent } from '../../../../app/components/audit-timeline/audit-timeline';

describe('AuditTimelineComponent', () => {
  let component: AuditTimelineComponent;
  let fixture: ComponentFixture<AuditTimelineComponent>;
  let auditCalls: Array<{ action?: string; fromDate?: string; toDate?: string }>;

  beforeEach(async () => {
    auditCalls = [];

    await TestBed.configureTestingModule({
      imports: [AuditTimelineComponent],
      providers: [
        {
          provide: AssetService,
          useValue: {
            getAuditTrail: (action?: string, fromDate?: string, toDate?: string) => {
              auditCalls.push({ action, fromDate, toDate });
              return of([{ action: 'ASSIGN', assetName: 'Dell Laptop', details: 'Assigned', timestamp: '2026-06-16T10:00:00Z', performedByEmail: 'admin@company.com' }]);
            },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AuditTimelineComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('loads logs on init and derives available actions', () => {
    auditCalls = [];

    component.ngOnInit();

    expect(auditCalls.length).toBe(1);
    expect(auditCalls[0].action).toBe('All');
    expect(component.logs.length).toBe(1);
    expect(component.availableActions).toContain('ASSIGN');
  });

  it('reloads logs on action and date changes', () => {
    auditCalls = [];

    component.selectedAction = 'ASSIGN';
    component.onActionChange();

    component.fromDate = '2026-06-01';
    component.toDate = '2026-06-30';
    component.onDateRangeChange();

    expect(auditCalls[0].action).toBe('ASSIGN');
    expect(auditCalls[1].fromDate).toBe('2026-06-01');
    expect(auditCalls[1].toDate).toBe('2026-06-30');
  });

  it('clears date range and reloads logs', () => {
    component.fromDate = '2026-06-01';
    component.toDate = '2026-06-30';

    component.clearDateRange();

    expect(component.fromDate).toBe('');
    expect(component.toDate).toBe('');
    expect(auditCalls[0].fromDate).toBe('');
  });

  it('maps known action labels and colors', () => {
    expect(component.getActionLabel('ASSIGN')).toBe('Asset Assigned');
    expect(component.getActionLabel('UNKNOWN')).toBe('UNKNOWN');
    expect(component.getActionColor('PROVISION')).toBe('#2b6cb0');
    expect(component.getActionColor('RETURN')).toBe('#dd6b20');
  });
});

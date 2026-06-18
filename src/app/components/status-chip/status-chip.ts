import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface StatusConfig {
  color: string;
  icon: string;
  label: string;
  bgColor: string;
}

// Centralized status configuration - SINGLE SOURCE OF TRUTH
const STATUS_MAP: { [key: string]: StatusConfig } = {
  'Available': {
    color: '#22863a',
    icon: '✓',
    label: 'Available',
    bgColor: '#f0fdf4'
  },
  'Checked Out': {
    color: '#0366d6',
    icon: '↗',
    label: 'Checked Out',
    bgColor: '#ebf8ff'
  },
  'Under Maintenance': {
    color: '#b45309',
    icon: '⚙',
    label: 'Under Maintenance',
    bgColor: '#fffbeb'
  },
  'Decommissioned': {
    color: '#57534e',
    icon: '✕',
    label: 'Decommissioned',
    bgColor: '#f5f5f4'
  }
};

@Component({
  selector: 'app-status-chip',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span class="status-chip" 
          [style.background-color]="statusConfig.bgColor"
          [style.color]="statusConfig.color"
          [style.border-color]="statusConfig.color">
      <span class="status-icon">{{ statusConfig.icon }}</span>
      <span class="status-label">{{ statusConfig.label }}</span>
    </span>
  `,
  styles: [`
    .status-chip {
      display: inline-flex;
      align-items: center;
      gap: 0.375rem;
      padding: 0.375rem 0.75rem;
      border-radius: 9999px;
      border: 1.5px solid;
      font-weight: 600;
      font-size: 0.8125rem;
      white-space: nowrap;
    }

    .status-icon {
      font-weight: bold;
      line-height: 1;
    }

    .status-label {
      letter-spacing: 0.2px;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StatusChipComponent {
  @Input() status = 'Available';

  get statusConfig(): StatusConfig {
    return STATUS_MAP[this.status] || STATUS_MAP['Available'];
  }

  // Export for use in templates and services
  static getStatusConfig(status: string): StatusConfig {
    return STATUS_MAP[status] || STATUS_MAP['Available'];
  }

  static getStatusMap(): { [key: string]: StatusConfig } {
    return { ...STATUS_MAP };
  }
}

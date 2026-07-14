import { CommonModule, DecimalPipe } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CreateClientAlertRequest } from '../../../../Models/client-finance-models/client-pattern-models';

interface LevelEntry {
  label: string;
  value: number;
  triggerHint: string;
}

@Component({
  selector: 'app-pattern-trigger-levels',
  standalone: true,
  imports: [CommonModule, DecimalPipe],
  templateUrl: './pattern-trigger-levels.component.html',
  styleUrl: './pattern-trigger-levels.component.scss'
})
export class PatternTriggerLevelsComponent {
  @Input() symbol = '';
  @Input() patternId = '';
  @Input() necklinePrice: number | null = null;
  @Input() targetPrice: number | null = null;
  @Input() invalidationPrice: number | null = null;

  @Output() createAlert = new EventEmitter<CreateClientAlertRequest>();

  get levels(): LevelEntry[] {
    const entries: LevelEntry[] = [];
    if (this.necklinePrice != null) {
      entries.push({ label: 'Neckline', value: this.necklinePrice, triggerHint: 'Croisement de la neckline' });
    }
    if (this.invalidationPrice != null) {
      entries.push({ label: 'Invalidation', value: this.invalidationPrice, triggerHint: 'Croisement du niveau d\'invalidation' });
    }
    return entries;
  }

  onCreateLevelAlert(level: LevelEntry): void {
    this.createAlert.emit({
      Symbol: this.symbol,
      Trigger: 'LevelCrossed',
      LevelValue: level.value,
      PatternId: null
    });
  }

  onCreateStateAlert(): void {
    if (!this.patternId) return;
    this.createAlert.emit({
      Symbol: this.symbol,
      Trigger: 'PatternStateChange',
      LevelValue: null,
      PatternId: this.patternId
    });
  }
}

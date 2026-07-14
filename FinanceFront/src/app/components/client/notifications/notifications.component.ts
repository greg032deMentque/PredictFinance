import { CommonModule, DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { UserPaths } from '../../../Routes/app.routes.constants';

interface NotificationItem {
  NotificationId: string;
  Category: number | string;
  Status: number | string;
  Title: string;
  Summary: string;
  CreatedAtUtc: string;
  TargetScreen?: number | string | null;
  TargetEntityId?: string | null;
  ReadAtUtc?: string | null;
  AlertTrigger?: number | string | null;
}

interface AlertTriggerMeta {
  label: string;
  icon: string;
  chipClass: string;
}

const ALERT_TRIGGER_META: Record<string, AlertTriggerMeta> = {
  PatternStateChange: { label: 'Pattern', icon: 'bi-diagram-3', chipClass: 'chip-navy' },
  '0': { label: 'Pattern', icon: 'bi-diagram-3', chipClass: 'chip-navy' },
  LevelCrossed: { label: 'Niveau franchi', icon: 'bi-arrow-up-down', chipClass: 'chip-warning' },
  '1': { label: 'Niveau franchi', icon: 'bi-arrow-up-down', chipClass: 'chip-warning' },
  DataStale: { label: 'Données obsolètes', icon: 'bi-clock-history', chipClass: 'chip-secondary' },
  '2': { label: 'Données obsolètes', icon: 'bi-clock-history', chipClass: 'chip-secondary' },
};

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe],
  templateUrl: './notifications.component.html',
  styleUrl: './notifications.component.scss'
})
export class NotificationsComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  notifications: NotificationItem[] = [];
  loading = false;
  selectedStatus = 'all';
  error: string | null = null;

  ngOnInit(): void {
    this.loadNotifications();
  }

  loadNotifications(): void {
    this.loading = true;
    this.error = null;

    const queryParts: string[] = ['take=50'];

    if (this.selectedStatus === 'unread') {
      queryParts.push('status=0');
    }

    if (this.selectedStatus === 'read') {
      queryParts.push('status=1');
    }

    this.http
      .get<NotificationItem[]>(`${environment.apiUrl}Notifications/GetList?${queryParts.join('&')}`)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (payload) => {
          this.notifications = payload ?? [];
        },
        error: () => {
          this.notifications = [];
          this.error = 'Impossible de charger les notifications.';
        }
      });
  }

  markAsRead(notification: NotificationItem): void {
    if (!notification.NotificationId || this.isRead(notification)) {
      return;
    }

    this.http
      .post<NotificationItem>(`${environment.apiUrl}Notifications/MarkAsRead`, {
        NotificationId: notification.NotificationId
      })
      .subscribe({
        next: (updated) => {
          this.notifications = this.notifications.map((item) =>
            item.NotificationId === updated.NotificationId ? updated : item
          );
        }
      });
  }

  isRead(notification: NotificationItem): boolean {
    return notification.ReadAtUtc !== null && notification.ReadAtUtc !== undefined && notification.ReadAtUtc !== '';
  }

  get unreadCount(): number {
    return this.notifications.filter((item) => !this.isRead(item)).length;
  }

  get filteredNotifications(): NotificationItem[] {
    if (this.selectedStatus === 'all') {
      return this.notifications;
    }

    if (this.selectedStatus === 'read') {
      return this.notifications.filter((item) => this.isRead(item));
    }

    return this.notifications.filter((item) => !this.isRead(item));
  }

  getAlertTriggerMeta(trigger: number | string | null | undefined): AlertTriggerMeta | null {
    if (trigger === null || trigger === undefined) return null;
    return ALERT_TRIGGER_META[String(trigger)] ?? null;
  }

  canNavigate(notification: NotificationItem): boolean {
    const screen = notification.TargetScreen;
    if (screen === null || screen === undefined) return false;
    const screenStr = String(screen);
    return (
      screenStr === 'InstrumentDetail' || screenStr === '0' ||
      screenStr === 'AnalysisResult' || screenStr === '1' ||
      screenStr === 'HelpCenter' || screenStr === '2' ||
      screenStr === 'Account' || screenStr === '3'
    );
  }

  openTarget(notification: NotificationItem): void {
    if (!this.isRead(notification)) {
      this.markAsRead(notification);
    }

    const screen = String(notification.TargetScreen ?? '');
    const entityId = notification.TargetEntityId ?? '';

    if (screen === 'InstrumentDetail' || screen === '0') {
      if (entityId) {
        void this.router.navigateByUrl('/' + UserPaths.InstrumentDetail(entityId));
      }
    } else if (screen === 'AnalysisResult' || screen === '1') {
      if (entityId) {
        void this.router.navigateByUrl('/' + UserPaths.AnalysisDetail(entityId));
      }
    } else if (screen === 'HelpCenter' || screen === '2') {
      void this.router.navigateByUrl('/' + UserPaths.HelpCenter);
    } else if (screen === 'Account' || screen === '3') {
      void this.router.navigateByUrl('/' + UserPaths.Account);
    }
  }
}

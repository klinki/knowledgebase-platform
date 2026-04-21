import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import {
  AuthService,
  SupportedLanguage,
  TelegramLinkStatus,
  UserLanguagePreferences
} from '../../core/services/auth.service';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss'
})
export class SettingsComponent implements OnInit, OnDestroy {
  private readonly authService = inject(AuthService);
  private countdownTimer: ReturnType<typeof setInterval> | null = null;

  loading = signal(true);
  saving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  defaultLanguageCode = signal('');
  preservedLanguageCodes = signal<string[]>([]);
  supportedLanguages = signal<SupportedLanguage[]>([]);

  telegramLoading = signal(false);
  telegramStatus = signal<TelegramLinkStatus | null>(null);
  telegramError = signal<string | null>(null);
  telegramCodeSecondsRemaining = signal(0);

  async ngOnInit(): Promise<void> {
    await this.loadPreferences();
    await this.loadTelegramStatus();
  }

  ngOnDestroy(): void {
    this.stopCountdown();
  }

  isPreserved(languageCode: string): boolean {
    return this.preservedLanguageCodes().includes(languageCode);
  }

  setDefaultLanguage(languageCode: string): void {
    this.defaultLanguageCode.set(languageCode);
    this.successMessage.set(null);
    this.preservedLanguageCodes.update(codes => codes.filter(code => code !== languageCode));
  }

  togglePreserved(languageCode: string, checked: boolean): void {
    this.successMessage.set(null);
    this.preservedLanguageCodes.update(codes => {
      const next = new Set(codes);
      if (checked) {
        next.add(languageCode);
      } else {
        next.delete(languageCode);
      }

      return Array.from(next).sort((left, right) => left.localeCompare(right));
    });
  }

  async save(): Promise<void> {
    this.saving.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    try {
      const preferences = await this.authService.updatePreferences({
        defaultLanguageCode: this.defaultLanguageCode(),
        preservedLanguageCodes: this.preservedLanguageCodes().filter(code => code !== this.defaultLanguageCode())
      });
      this.applyPreferences(preferences);
      this.successMessage.set('Language preferences saved.');
    } catch {
      this.errorMessage.set('Saving language preferences failed. Try again.');
    } finally {
      this.saving.set(false);
    }
  }

  async issueTelegramCode(): Promise<void> {
    this.telegramLoading.set(true);
    this.telegramError.set(null);

    try {
      await this.authService.issueTelegramLinkCode();
      await this.loadTelegramStatus();
    } catch {
      this.telegramError.set('Unable to issue Telegram link code.');
    } finally {
      this.telegramLoading.set(false);
    }
  }

  async unlinkTelegram(): Promise<void> {
    this.telegramLoading.set(true);
    this.telegramError.set(null);

    try {
      await this.authService.unlinkTelegram();
      await this.loadTelegramStatus();
    } catch {
      this.telegramError.set('Unable to unlink Telegram chat.');
    } finally {
      this.telegramLoading.set(false);
    }
  }

  async refreshTelegramStatus(): Promise<void> {
    await this.loadTelegramStatus();
  }

  private async loadPreferences(): Promise<void> {
    this.loading.set(true);
    this.errorMessage.set(null);

    try {
      const preferences = await this.authService.getPreferences();
      this.applyPreferences(preferences);
    } catch {
      this.errorMessage.set('Loading language preferences failed.');
    } finally {
      this.loading.set(false);
    }
  }

  private async loadTelegramStatus(): Promise<void> {
    this.telegramLoading.set(true);
    this.telegramError.set(null);

    try {
      const status = await this.authService.getTelegramStatus();
      this.telegramStatus.set(status);
      this.startCountdown(status);
    } catch {
      this.telegramError.set('Unable to load Telegram status.');
    } finally {
      this.telegramLoading.set(false);
    }
  }

  private applyPreferences(preferences: UserLanguagePreferences): void {
    this.defaultLanguageCode.set(preferences.defaultLanguageCode);
    this.preservedLanguageCodes.set([...preferences.preservedLanguageCodes]);
    this.supportedLanguages.set([...preferences.supportedLanguages]);
  }

  private startCountdown(status: TelegramLinkStatus): void {
    this.stopCountdown();
    const expiresAt = status.pendingCode?.expiresAt;
    if (!expiresAt) {
      this.telegramCodeSecondsRemaining.set(0);
      return;
    }

    const update = (): void => {
      const ms = new Date(expiresAt).getTime() - Date.now();
      this.telegramCodeSecondsRemaining.set(Math.max(0, Math.floor(ms / 1000)));
    };

    update();
    this.countdownTimer = setInterval(update, 1000);
  }

  private stopCountdown(): void {
    if (this.countdownTimer) {
      clearInterval(this.countdownTimer);
      this.countdownTimer = null;
    }
  }
}

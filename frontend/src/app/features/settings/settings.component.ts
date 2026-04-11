import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import {
  AuthService,
  SupportedLanguage,
  UserLanguagePreferences
} from '../../core/services/auth.service';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss'
})
export class SettingsComponent implements OnInit {
  private readonly authService = inject(AuthService);

  loading = signal(true);
  saving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  defaultLanguageCode = signal('');
  preservedLanguageCodes = signal<string[]>([]);
  supportedLanguages = signal<SupportedLanguage[]>([]);

  async ngOnInit(): Promise<void> {
    await this.loadPreferences();
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

  private applyPreferences(preferences: UserLanguagePreferences): void {
    this.defaultLanguageCode.set(preferences.defaultLanguageCode);
    this.preservedLanguageCodes.set([...preferences.preservedLanguageCodes]);
    this.supportedLanguages.set([...preferences.supportedLanguages]);
  }
}

import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { SettingsComponent } from './settings.component';
import { AuthService } from '../../core/services/auth.service';

describe('SettingsComponent', () => {
  it('loads preferences and saves updated language settings', async () => {
    const currentUser = signal({
      id: 'user-1',
      email: 'member@example.com',
      displayName: 'Member',
      role: 'member',
      defaultLanguageCode: 'en',
      preservedLanguageCodes: ['fr']
    });

    const updatePreferences = vi.fn().mockImplementation(async request => {
      currentUser.set({
        ...currentUser()!,
        defaultLanguageCode: request.defaultLanguageCode,
        preservedLanguageCodes: request.preservedLanguageCodes
      });

      return {
        defaultLanguageCode: request.defaultLanguageCode,
        preservedLanguageCodes: request.preservedLanguageCodes,
        supportedLanguages: [
          { code: 'de', displayName: 'German' },
          { code: 'en', displayName: 'English' },
          { code: 'fr', displayName: 'French' }
        ]
      };
    });

    const authServiceStub = {
      currentUser,
      getPreferences: vi.fn().mockResolvedValue({
        defaultLanguageCode: 'en',
        preservedLanguageCodes: ['fr'],
        supportedLanguages: [
          { code: 'de', displayName: 'German' },
          { code: 'en', displayName: 'English' },
          { code: 'fr', displayName: 'French' }
        ]
      }),
      updatePreferences
    };

    await TestBed.configureTestingModule({
      imports: [SettingsComponent],
      providers: [
        {
          provide: AuthService,
          useValue: authServiceStub
        }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(SettingsComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    const component = fixture.componentInstance;
    expect(component.defaultLanguageCode()).toBe('en');
    expect(component.preservedLanguageCodes()).toEqual(['fr']);

    component.setDefaultLanguage('de');
    component.togglePreserved('en', true);
    await component.save();
    fixture.detectChanges();

    expect(updatePreferences).toHaveBeenCalledWith({
      defaultLanguageCode: 'de',
      preservedLanguageCodes: ['en', 'fr']
    });
    expect(currentUser().defaultLanguageCode).toBe('de');
    expect(currentUser().preservedLanguageCodes).toEqual(['en', 'fr']);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Language preferences saved.');
  });
});

import { TestBed } from '@angular/core/testing';
import { THEME_STORAGE_KEY, ThemeService } from './theme';

describe('ThemeService', () => {
  const root = document.documentElement;

  beforeEach(() => {
    localStorage.clear();
    root.removeAttribute('data-theme');
  });

  afterEach(() => {
    vi.restoreAllMocks();
    localStorage.clear();
    root.removeAttribute('data-theme');
  });

  function service(): ThemeService {
    return TestBed.inject(ThemeService);
  }

  it('restore_NothingStored_FollowsTheOs', () => {
    service().restore();

    expect(service().preference()).toBe('system');
    expect(root.hasAttribute('data-theme')).toBe(false);
  });

  it('restore_DarkStored_SetsTheAttribute', () => {
    localStorage.setItem(THEME_STORAGE_KEY, 'dark');

    service().restore();

    expect(service().preference()).toBe('dark');
    expect(root.getAttribute('data-theme')).toBe('dark');
  });

  it('restore_UnknownValueStored_FollowsTheOs', () => {
    localStorage.setItem(THEME_STORAGE_KEY, 'purple');

    service().restore();

    expect(service().preference()).toBe('system');
    expect(root.hasAttribute('data-theme')).toBe(false);
  });

  it('set_Light_StoresAndApplies', () => {
    service().set('light');

    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe('light');
    expect(root.getAttribute('data-theme')).toBe('light');
  });

  it('set_SystemAfterDark_ClearsStorageAndAttribute', () => {
    service().set('dark');

    service().set('system');

    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBeNull();
    expect(root.hasAttribute('data-theme')).toBe(false);
  });

  it('set_StorageBlocked_StillAppliesForThisVisit', () => {
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new DOMException('Blocked', 'SecurityError');
    });

    service().set('dark');

    expect(service().preference()).toBe('dark');
    expect(root.getAttribute('data-theme')).toBe('dark');
  });

  it('restore_StorageBlocked_FollowsTheOs', () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new DOMException('Blocked', 'SecurityError');
    });

    service().restore();

    expect(service().preference()).toBe('system');
  });
});

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Pagination } from './pagination';

describe('Pagination', () => {
  let fixture: ComponentFixture<Pagination>;

  function render(page: number, totalCount: number): HTMLElement {
    fixture = TestBed.createComponent(Pagination);
    fixture.componentRef.setInput('page', page);
    fixture.componentRef.setInput('pageSize', 20);
    fixture.componentRef.setInput('totalCount', totalCount);
    fixture.componentRef.setInput('totalPages', Math.ceil(totalCount / 20));
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  function button(root: HTMLElement, label: string): HTMLButtonElement {
    const element = root.querySelector<HTMLButtonElement>(`button[aria-label="${label}"]`);
    if (!element) throw new Error(`No button ${label}`);
    return element;
  }

  it('render_FirstPage_ShowsRangeAndDisablesPrevious', () => {
    const root = render(1, 34);

    expect(root.textContent).toContain('Showing 1–20 of 34');
    expect(root.textContent).toContain('Page 1 of 2');
    expect(button(root, 'Previous page').disabled).toBe(true);
    expect(button(root, 'Next page').disabled).toBe(false);
  });

  it('render_LastPage_ShowsPartialRangeAndDisablesNext', () => {
    const root = render(2, 34);

    expect(root.textContent).toContain('Showing 21–34 of 34');
    expect(button(root, 'Next page').disabled).toBe(true);
  });

  it('next_Clicked_EmitsTheFollowingPage', () => {
    const root = render(1, 34);
    const pages: number[] = [];
    fixture.componentInstance.pageChange.subscribe((page) => pages.push(page));

    button(root, 'Next page').click();

    expect(pages).toEqual([2]);
  });

  it('render_Disabled_DisablesBothButtons', () => {
    const root = render(2, 60);
    fixture.componentRef.setInput('disabled', true);
    fixture.detectChanges();

    expect(button(root, 'Previous page').disabled).toBe(true);
    expect(button(root, 'Next page').disabled).toBe(true);
  });
});

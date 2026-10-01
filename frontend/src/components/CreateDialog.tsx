import { useEffect, useRef, useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { ArrowRight, X } from 'lucide-react';
import { api } from '../api/orders';
import { errorMessage } from '../utils/error';
import { useLive } from '../store/live';

export default function CreateDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const dialog = useRef<HTMLDialogElement>(null);
  const navigate = useNavigate();
  const [number, setNumber] = useState('');
  const [description, setDescription] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  useEffect(() => {
    if (open) {
      setNumber('');
      setDescription('');
      setError('');
      dialog.current?.showModal();
    } else dialog.current?.close();
  }, [open]);
  async function submit(event: FormEvent) {
    event.preventDefault();
    if (!number.trim() || !description.trim() || busy) return;
    setBusy(true);
    setError('');
    try {
      const order = await api.create(number.trim(), description.trim());
      useLive.getState().refresh();
      onClose();
      navigate(`/orders/${order.id}`);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }
  return (
    <dialog
      ref={dialog}
      onCancel={(event) => {
        event.preventDefault();
        if (!busy) onClose();
      }}
      aria-labelledby="create-title"
    >
      <div className="dialog-top">
        <button className="icon-button" onClick={onClose} disabled={busy} aria-label="Закрыть">
          <X size={21} />
        </button>
      </div>
      <h2 id="create-title">Новый заказ</h2>
      <form onSubmit={submit}>
        <label htmlFor="number">Номер заказа</label>
        <input
          id="number"
          autoFocus
          required
          maxLength={64}
          value={number}
          onChange={(e) => setNumber(e.target.value)}
          placeholder="Например, ORD-2026-001"
          disabled={busy}
        />
        <label htmlFor="description">Описание</label>
        <textarea
          id="description"
          required
          maxLength={2000}
          rows={4}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder="Что входит в заказ?"
          disabled={busy}
        />
        <div className="field-hint">{description.length} / 2000</div>
        {error && (
          <div className="error" role="alert">
            {error}
          </div>
        )}
        <div className="dialog-actions">
          <button type="button" className="button secondary" onClick={onClose} disabled={busy}>
            Отмена
          </button>
          <button className="button" disabled={busy || !number.trim() || !description.trim()}>
            {busy ? 'Сохранение…' : 'Создать заказ'}
            <ArrowRight size={17} />
          </button>
        </div>
      </form>
    </dialog>
  );
}
